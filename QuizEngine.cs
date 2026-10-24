using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// LukiQuizEngine 2.2.0
public class CPHInline
{
    private const string AppVersion = "2.2.0";
    private const string BuildLabel = "PHoeniX"; // don't change
    private const string EnginePatch = "QUPA"; // don't change
    private const int Protocol = 1;
    private const string PointsCommand = "!punkty"; // use panel.html to change this in options
    private const string RankingCommand = "!ranking"; // use panel.html to change this in options
    private const string AdminCommand = "!quiz"; // use panel.html to change this in options
    private const string DataVariable = "localQuiz_data_v2"; // don't change
    private const string PublicVariable = "localQuiz_public_v2"; // don't change
    private const string SnapshotVariable = "localQuiz_snapshots_v2"; // don't change
    private const int MaxBytes = 8 * 1024 * 1024;
    private const int MaxImportedPayloadLength = 7500000;
    private const int MaxImportedScores = 25000;
    private const int MaxUserIdLength = 160;
    private const int MaxHistory = 100;
    private const int MaxArchiveCleanupItems = 100;
    private const int MaxSnapshotBytes = 32 * 1024 * 1024;
    private static readonly object StateLock = String.Intern("LukiQuizEngine.v2.transaction");
    private static readonly Dictionary<Type,Dictionary<string,PropertyInfo>> propertyCache=new Dictionary<Type,Dictionary<string,PropertyInfo>>();
    private static readonly Regex VoteRegex = new Regex(@"^!(1[0-2]|[1-9])$", RegexOptions.CultureInvariant);
    private readonly Dictionary<string,ArchiveCleanupPlan> archiveCleanupPlans = new Dictionary<string,ArchiveCleanupPlan>();
    private Timer roundTimer;
    private Root quizData;
    private string storedStateJson;
    private string failedExpirationStateJson;
    private string expirationError = "";
    private Dictionary<string,string> requestArgs = new Dictionary<string,string>();
    private string requestId = "";
    private string requestOperation = "";
    private bool legacyCountersRemoved;
    private bool requestFailed;
    private ArchiveResetResult resetArchive;
    private int resetResultRevision = -1;
    private bool resetStateSaved;
    private ArchiveSaveResult archiveSave;
    private ArchiveRestoreResult restoreArchive;
    private ArchiveCleanupResult archiveCleanup;
    private int archiveResultRevision = -1;
    private static readonly string[] ArgumentNames = {
        "quizOperation","lqeMutation","lqeRequestId","lqeConfirm","lqeScoreUserId","lqeScoreUserName","lqeNewPoints",
        "confirmText","userId","userName","user","broadcastUserId","isBroadcaster","isModerator","isMod","isInternal","message",
        "newPoints","roundId","payloadJson","question","answersJson","durationSeconds","correctIndex","pointsPerCorrect","pointsPerAdjacent",
        "scoringMode","partialIndicesJson","scoresJson","importMode","itemId","shuffle","answerIndex","seconds"
    };
    private bool disposed;
    private long lastBroadcastAt;
    private bool stateBroadcastPending;
    private readonly Dictionary<string, long> commandCooldowns = new Dictionary<string, long>();
    private readonly JsonSerializerSettings serializerOptions = new JsonSerializerSettings {
        TypeNameHandling = TypeNameHandling.None, MissingMemberHandling = MissingMemberHandling.Error,
        MaxDepth = 64, DateParseHandling = DateParseHandling.None
    };

    public void Init() { EnsureTimer(); lock(StateLock) { try { if(!String.IsNullOrWhiteSpace(CPH.GetGlobalVar<string>(DataVariable,true))) {quizData=LoadState();if(TryExpireAndSave()){BroadcastState();}} }catch(Exception ex){quizData=null;CPH.LogError("[LukiQuiz V2] Init: "+ex.GetType().Name);} } }
    public void Dispose() { lock (StateLock) { disposed = true; if (roundTimer != null) roundTimer.Dispose(); roundTimer = null; quizData = null; } }
    private void EnsureTimer() {
        lock (StateLock) { if (!disposed && roundTimer == null) roundTimer = new Timer(_ => ClockTick(), null, 250, 250); }
    }
    private void ClockTick() {
        lock (StateLock) {
            if (disposed || quizData == null) return;
            try {
                quizData = LoadState();
                if (TryExpireAndSave()) { BroadcastState(); }
                else if (stateBroadcastPending && Now() - lastBroadcastAt >= 100) BroadcastState();
            } catch (Exception ex) { quizData = null; CPH.LogError("[LukiQuiz V2] Timer stopped after error: " + ex.GetType().Name); }
        }
    }
    public bool Execute() {
        string wire = ReadHostArg("quizRequest");
        var captured = new Dictionary<string,string>();
        if (String.IsNullOrWhiteSpace(wire)) foreach (string key in ArgumentNames) captured[key] = ReadHostArg(key);
        lock (StateLock) {
            requestArgs = captured; requestId = ""; requestOperation = ""; requestFailed = false;
            resetArchive = null; resetResultRevision = -1; resetStateSaved = false;
            archiveSave = null; restoreArchive = null; archiveCleanup = null; archiveResultRevision = -1;
            string op = "";
            try {
                if (!String.IsNullOrWhiteSpace(wire)) {
                    var request = Read<PanelRequest>(wire);
                    Require(request != null && request.Protocol == Protocol && request.Build == BuildLabel, "Niezgodna wersja panelu i silnika. Odśwież oba pliki.");
                    Require(Regex.IsMatch(request.Id ?? "", @"^[a-zA-Z0-9_-]{1,80}$"), "Nieprawidłowe ID żądania.");
                    requestId = request.Id;
                    Require(request.Args != null && request.Args.Count <= 40, "Nieprawidłowe argumenty.");
                    requestArgs = request.Args;
                    op = request.Operation;
                    Require(!String.IsNullOrEmpty(op),"Brak operacji panelu.");
                } else {
                    string legacyRequestId=GetStringArg("lqeRequestId");
                    if(legacyRequestId!="") {
                        Require(Regex.IsMatch(legacyRequestId,@"^[a-zA-Z0-9_-]{1,80}$"),"Nieprawidłowe ID żądania.");
                        requestId=legacyRequestId;
                    }
                    op = NormalizeLegacyOperation();
                }
                Require(op != null && op.Length <= 40 && op.All(c => c >= 'a' && c <= 'z'), "Nieprawidłowa operacja.");
                requestOperation=op;
                if (requestId != "") {
                    Broadcast(new {type="quiz-receipt",requestId=requestId,build=BuildLabel,operation=op});
                    if (!IsReadOperation(op)) CPH.LogInfo("[LukiQuiz V2] RECEIVED build="+BuildLabel+" request="+requestId+" op="+op);
                }
                if (op=="openarchivefolder") {
                    Require(!String.IsNullOrWhiteSpace(wire) && requestArgs.Count==0 && String.IsNullOrWhiteSpace(ReadHostArg("message")) && String.IsNullOrWhiteSpace(ReadHostArg("userId")),"Folder archiwów można otworzyć wyłącznie przyciskiem w panelu, bez dodatkowych argumentów.");
                    int revision=ReadArchiveStateRevision();
                    string directory=OpenRankingArchiveFolder();
                    Broadcast(new {type="quiz-result",requestId=requestId,build=BuildLabel,operation=op,ok=true,revision=revision,archiveFolder=new {status="opened",path=directory}});
                    return true;
                }
                if(new[]{"savearchive","listarchives","previewarchive","restorearchive","statusarchive","previewarchivecleanup","deletearchives"}.Contains(op)) {
                    Require(!String.IsNullOrWhiteSpace(wire) && String.IsNullOrWhiteSpace(ReadHostArg("message")) && String.IsNullOrWhiteSpace(ReadHostArg("userId")),"Archiwum rankingu jest dostępne wyłącznie przez panel.");
                    var previousData=quizData;string previousRaw=storedStateJson;
                    try {
                        quizData=ReadArchiveState();
                        HandleArchiveOperation(op);
                        Broadcast(new {type="quiz-result",requestId=requestId,build=BuildLabel,operation=op,ok=true,revision=archiveResultRevision>=0?archiveResultRevision:quizData.Revision,archiveSave=archiveSave,restoreArchive=restoreArchive,archiveCleanup=archiveCleanup});
                    }finally {
                        if(op!="restorearchive"||restoreArchive==null||restoreArchive.replayed){quizData=previousData;storedStateJson=previousRaw;}
                    }
                    return true;
                }
                if(op=="getcompetitive") {
                    var previousData=quizData;string previousRaw=storedStateJson;
                    try {quizData=ReadArchiveState();BroadcastCompetitive();Broadcast(new{type="quiz-result",requestId=requestId,build=BuildLabel,operation=op,ok=true,revision=quizData.Revision});}
                    finally {quizData=previousData;storedStateJson=previousRaw;}
                    return true;
                }
                EnsureTimer();
                quizData = LoadState();
                TryExpireAndSave();
                if (op != "") HandlePanelOperation(op); else HandleTwitchChat();
                if (requestId != "" && !requestFailed) {
                    Broadcast(new {type="quiz-result",requestId=requestId,build=BuildLabel,operation=op,ok=true,revision=resetResultRevision>=0?resetResultRevision:quizData.Revision,resetArchive=resetArchive});
                    if (!IsReadOperation(op)) CPH.LogInfo("[LukiQuiz V2] COMMITTED build="+BuildLabel+" request="+requestId+" op="+op+" revision="+quizData.Revision);
                }
            } catch (Exception ex) {
                if(!new[]{"openarchivefolder","savearchive","listarchives","previewarchive","statusarchive"}.Contains(op))quizData = null;
                CPH.LogError("[LukiQuiz V2] FAILED build="+BuildLabel+" request="+requestId+" error="+ex.GetType().Name);
                string failure = "Operacja odrzucona: " + SafeError(ex);
                if (op=="resetscores" && resetArchive!=null && resetArchive.status=="archived")
                    failure = "Archiwum zapisane: " + resetArchive.path + ". " + (resetStateSaved ? "Reset zapisany, ale nie dostarczono potwierdzenia. " : "Nie potwierdzono zatwierdzenia resetu w Streamer.bot. ") + "Odśwież ranking. Ponów to samo żądanie, aby sprawdzić wynik.";
                BroadcastError(failure);
            } finally { requestArgs = new Dictionary<string,string>(); requestId = ""; requestOperation = ""; }
        }
        return true;
    }
    private static bool IsReadOperation(string op) { return new[]{"sync","tick","getscores","getdata","diagnostics","exportbackup","exportbank","previewbackup","previewbank","openarchivefolder","listarchives","previewarchive","statusarchive","previewarchivecleanup","getcompetitive"}.Contains(op); }
    private string SafeError(Exception ex) { return ex is InvalidOperationException ? ex.Message : "Nieprawidłowe dane lub błąd zapisu. Sprawdź Diagnostykę."; }
    private string ReadHostArg(string name) {
        object value;
        return CPH.TryGetArg(name,out value) && value != null ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? "" : "";
    }
    private string NormalizeLegacyOperation() {
        string op=GetStringArg("quizOperation").Trim().ToLowerInvariant();
        string raw=GetStringArg("quizOperation").Trim();
        if(op.StartsWith("rankreset|")) {
            var parts=raw.Split('|');Require(parts.Length==3,"Nieprawidłowy reset legacy.");
            if(requestId=="" && Regex.IsMatch(parts[2],@"^[a-zA-Z0-9_-]{1,80}$"))requestId="legacy-"+parts[2];
            requestArgs["confirmText"]=parts[1];return "resetscores";
        }
        if(op.StartsWith("rankset|")) {
            var parts=raw.Split('|');Require(parts.Length==5,"Nieprawidłowa korekta legacy.");
            requestArgs["userId"]=Uri.UnescapeDataString(parts[1]);requestArgs["newPoints"]=parts[2];return "setscore";
        }
        if(op!=""&&op!="request")return op;
        string mutation=GetStringArg("lqeMutation").ToLowerInvariant();
        if(mutation!="") {
            if(mutation=="reset"||mutation=="resetscores") {requestArgs["confirmText"]=GetStringArg("lqeConfirm")==""?GetStringArg("confirmText"):GetStringArg("lqeConfirm");return "resetscores";}
            Require(mutation=="set"||mutation=="setscore","Nieznana mutacja legacy.");
            requestArgs["userId"]=GetStringArg("lqeScoreUserId");requestArgs["newPoints"]=GetStringArg("lqeNewPoints");return "setscore";
        }
        return op;
    }
    public class PanelRequest {
        public int Protocol {get;set;}
        public string Build {get;set;}
        public string Id {get;set;}
        public string Operation {get;set;}
        public Dictionary<string,string> Args {get;set;}
    }
    private static long Now() { return (long)(DateTime.UtcNow - new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalMilliseconds; }
    private static string Id() { return Guid.NewGuid().ToString("N"); }
    private static void Require(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private T Copy<T>(T obj) { return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj), serializerOptions); }
    private T Read<T>(string json, int maxBytes = MaxBytes) {
        legacyCountersRemoved=false;
        Require(!String.IsNullOrWhiteSpace(json) && Encoding.UTF8.GetByteCount(json) <= maxBytes, "Pusty lub zbyt duży JSON (max 8 MiB).");
        JToken token;
        using(var scan=new JsonTextReader(new System.IO.StringReader(json))) {
            scan.MaxDepth=64;var objects=new Stack<HashSet<string>>();
            while(scan.Read()) {
                Require(scan.TokenType != JsonToken.Comment,"Komentarze JSON są niedozwolone.");
                if(scan.TokenType==JsonToken.StartObject)objects.Push(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
                else if(scan.TokenType==JsonToken.EndObject)objects.Pop();
                else if(scan.TokenType==JsonToken.PropertyName)Require(objects.Peek().Add((string)scan.Value),"Zduplikowane pole JSON.");
            }
        }
        using (var reader = new JsonTextReader(new System.IO.StringReader(json))) {
            reader.MaxDepth = 64; reader.DateParseHandling = DateParseHandling.None;
            token = JToken.ReadFrom(reader);
            Require(!reader.Read(), "Dodatkowe dane za JSON.");
        }
        CheckTypes(token, typeof(T), 0, typeof(T)==typeof(PanelRequest)||typeof(T)==typeof(Root)||typeof(T)==typeof(Backup)||typeof(T)==typeof(BankEnvelope)||typeof(T)==typeof(Question)||typeof(T)==typeof(QuizSet)||typeof(T)==typeof(Settings)||typeof(T)==typeof(CompetitiveSettings)||typeof(T)==typeof(RankingArchiveDocument)||typeof(T)==typeof(ResetArchiveIntent)||typeof(T)==typeof(RankingOperationIntent));
        return token.ToObject<T>(JsonSerializer.Create(serializerOptions));
    }
    private void CheckTypes(JToken token, Type type, int depth, bool strictSchema) {
        Require(depth <= 64, "Za głęboki JSON.");
        Type nullable = Nullable.GetUnderlyingType(type);
        if (token.Type == JTokenType.Null) { Require(nullable != null || !type.IsValueType, "Nieprawidłowe null."); return; }
        if (nullable != null) type = nullable;
        if (type == typeof(string)) { Require(token.Type == JTokenType.String, "Oczekiwano tekstu."); return; }
        if (type == typeof(bool)) { Require(token.Type == JTokenType.Boolean, "Oczekiwano ON/OFF."); return; }
        if (type == typeof(int) || type == typeof(long)) { Require(token.Type == JTokenType.Integer, "Oczekiwano liczby całkowitej."); return; }
        if (type == typeof(double)) {Require(token.Type==JTokenType.Integer||token.Type==JTokenType.Float,"Expected a numeric value.");double value=token.Value<double>();Require(!Double.IsNaN(value)&&!Double.IsInfinity(value),"Invalid numeric value.");return;}
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) {
            Require(token.Type == JTokenType.Array && token.Count() <= 25000, "Nieprawidłowa lista.");
            foreach (var x in token) CheckTypes(x, type.GetGenericArguments()[0], depth+1,strictSchema); return;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
            Require(token.Type == JTokenType.Object && token.Count() <= 25000, "Nieprawidłowa mapa.");
            foreach (var p in ((JObject)token).Properties()) { ValidateText(p.Name,1,160,"Klucz"); CheckTypes(p.Value,type.GetGenericArguments()[1],depth+1,strictSchema); } return;
        }
        Require(token.Type == JTokenType.Object, "Oczekiwano obiektu.");
        if(typeof(ScoreEntry).IsAssignableFrom(type)) {
            foreach(var legacy in ((JObject)token).Properties().Where(p=>String.Equals(p.Name,"CurrentStreak",StringComparison.OrdinalIgnoreCase)||String.Equals(p.Name,"BestStreak",StringComparison.OrdinalIgnoreCase)).ToList()) {
                Require(legacy.Value.Type==JTokenType.Integer,"Nieprawidłowa statystyka starszego pliku.");
                ValidateRange(legacy.Value.Value<long>(),0,100000000,"Starsza statystyka");
                legacy.Remove(); legacyCountersRemoved=true;
            }
        }
        Dictionary<string,PropertyInfo> props;if(!propertyCache.TryGetValue(type,out props)){props=type.GetProperties().ToDictionary(p=>p.Name,p=>p,StringComparer.OrdinalIgnoreCase);propertyCache[type]=props;}
        var source=((JObject)token).Properties().ToDictionary(p=>p.Name,p=>p.Value,StringComparer.OrdinalIgnoreCase);
        if(strictSchema || type==typeof(Root) || type==typeof(Backup) || type==typeof(BankEnvelope)) {
            foreach(string required in props.Keys) {
                if(type==typeof(RankingArchiveDocument)&&new[]{"Name","ArchiveType","BaseScores","TotalPoints","ContentHash"}.Contains(required))continue;
                if(type==typeof(Settings)&&new[]{"PanelLanguage","OverlayLanguage","SyncLanguage","VoteParsing","ArchiveRetention","PointsCommand","RankingCommand","AdminCommand","AnnounceRoundEventsInChat"}.Contains(required))continue;
                if(type==typeof(Root)&&new[]{"Competitive","PreparedQueueIndex"}.Contains(required))continue;
                if(type==typeof(Round)&&new[]{"Competitive","Summary"}.Contains(required))continue;
                if(type==typeof(CompetitiveSettings)&&required=="SeasonsEnabled")continue;
                Require(source.ContainsKey(required), "Niepełny schemat: "+required);
            }
        }
        foreach (var p in ((JObject)token).Properties()) {
            PropertyInfo prop;Require(props.TryGetValue(p.Name,out prop), "Nieznane pole JSON.");
            CheckTypes(p.Value, prop.PropertyType, depth+1,strictSchema);
        }
    }
    private void ValidateText(string value, int min, int max, string label) {
        Require(value != null && value.Length >= min && value.Length <= max && !value.Any(c => Char.IsControl(c) && c != '\n' && c != '\r' && c != '\t'), label + ": nieprawidłowy tekst.");
    }
    private void Identifier(string value,bool empty=false) {ValidateText(value,empty?0:1,80,"ID");Require(!value.Any(Char.IsControl)&&(value==""&&empty||!String.IsNullOrWhiteSpace(value)),"Nieprawidłowe ID.");}
    private void UserId(string value) { ValidateText(value,1,160,"User ID"); Require(!value.Any(Char.IsControl) && !HasSpreadsheetFormulaPrefix(value),"Nieprawidłowe User ID."); }
    private void ValidateRange(long n, long min, long max, string label) { Require(n >= min && n <= max,label + ": nieprawidłowy zakres."); }
    private void ValidateScore(ScoreEntry x, string key) {
        Require(x != null,"Pusty wynik."); UserId(key); Require(x.UserId == key,"Niezgodne ID wyniku.");
        ValidateText(x.UserName,1,80,"Nazwa widza"); ValidateRange(x.Points,0,100000000,"Punkty");
        ValidateRange(x.AnsweredQuestions,0,100000000,"Odpowiedziane"); ValidateRange(x.CorrectAnswers,0,x.AnsweredQuestions,"Poprawne");
        ValidateRange(x.LastUpdatedAt,0,4102444800000L,"Timestamp");
    }
    private void ValidateQuestion(Question q, bool draft = false) {
        Require(q != null && q.Options != null && q.Tags != null,"Puste pytanie."); Identifier(q.Id);
        ValidateText(q.Name,0,80,"Nazwa"); ValidateText(q.Text,draft?0:2,500,"Pytanie"); ValidateText(q.Category,0,80,"Kategoria");
        Require(q.Tags.Count <= 20,"Za dużo tagów."); foreach (string t in q.Tags) ValidateText(t,1,40,"Tag");
        Require(q.Options.Count >= 2 && q.Options.Count <= 12,"Wymagane 2-12 odpowiedzi.");
        var ids = new HashSet<string>(); foreach (var o in q.Options) {
            Require(o != null,"Pusta odpowiedź."); Identifier(o.Id); Require(ids.Add(o.Id),"Duplikat ID odpowiedzi.");
            ValidateRange(o.AuthorIndex,0,12,"Author index"); ValidateText(o.Text,draft?0:1,240,"Odpowiedź"); if (o.Points.HasValue) ValidateRange(o.Points.Value,0,1000,"Punkty odpowiedzi");
        }
        Require(q.Mode == "QUIZ" || q.Mode == "POLL","Nieprawidłowy tryb rundy.");
        Require(new[]{"LIVE","COUNT_ONLY","HIDDEN"}.Contains(q.Voting),"Nieprawidłowy blind voting.");
        Require(new[]{"LAST","FIRST","LIMITED"}.Contains(q.Policy),"Nieprawidłowa polityka głosu.");
        Require(new[]{"adjacent","manual","none","custom"}.Contains(q.Scoring),"Nieprawidłowa punktacja.");
        ValidateRange(q.Duration,0,3600,"Timer"); ValidateRange(q.ChangeLimit,0,20,"Zmiany"); ValidateRange(q.FullPoints,0,1000,"Pełne punkty"); ValidateRange(q.PartialPoints,0,q.FullPoints,"Częściowe punkty");
        ValidateRange(q.SpeedFast,0,1000,"Bonus 5s"); ValidateRange(q.SpeedSlow,0,q.SpeedFast,"Bonus 10s");
        Require(!q.AutoReveal || (q.Duration > 0 && (q.Mode == "POLL" || q.Options.Any(o=>o.Correct))),"Auto Reveal wymaga timera i poprawnej odpowiedzi (QUIZ).");
        Require(!q.Speed || q.Mode == "QUIZ","Bonus szybkości nie dotyczy POLL.");
    }
    private void ValidateRound(Round r) {
        Require(r != null && r.Votes != null && r.Awards != null,"Pusta runda."); Identifier(r.Id); ValidateQuestion(r.Question);
        Require(new[]{"open","locked","revealed","cancelled"}.Contains(r.Status),"Status rundy.");
        ValidateRange(r.StartedAt,1,4102444800000L,"Start"); ValidateRange(r.ClosesAt,0,4102444800000L,"Zamknięcie"); ValidateRange(r.RemainingMs,0,3600000,"Pozostały czas");
        ValidateRange(r.ElapsedMs,0,4102444800000L,"Elapsed"); ValidateRange(r.ClockAt,0,4102444800000L,"Clock"); ValidateRange(r.EndedAt,0,4102444800000L,"Koniec");
        Require(r.Votes.Count <= 25000,"Za dużo głosów.");
        foreach (var v in r.Votes) { UserId(v.Key); Require(v.Value != null,"Pusty głos.");
            ValidateText(v.Value.Name,1,80,"Widz"); Identifier(v.Value.OptionId); Require(r.Question.Options.Any(o=>o.Id==v.Value.OptionId),"Głos na nieistniejącą odpowiedź.");
            ValidateRange(v.Value.Changes,0,100000000,"Zmiany"); ValidateRange(v.Value.ElapsedMs,0,4102444800000L,"Czas głosu"); Identifier(v.Value.TeamId,true);
        }
        foreach (var a in r.Awards) { UserId(a.Key); Require(a.Value != null && r.Votes.ContainsKey(a.Key),"Nieprawidłowe award."); ValidateRange(a.Value.Points,0,2000,"Award"); ValidateScore(a.Value.Before,a.Key); ValidateScore(a.Value.After,a.Key); }
        Require(!r.Applied || (r.Status == "revealed" && r.Question.Mode == "QUIZ"),"Nieprawidłowe rozliczenie.");
        if(r.Competitive!=null) {ValidateCompetitiveSettings(r.Competitive.Settings);Identifier(r.Competitive.SeasonId,true);}
        if(r.Status=="revealed"||r.Status=="cancelled")UpdateRoundSummary(r);
    }
    private void ValidateSettings(Settings s) {
        Require(s != null && s.Keys != null && s.Overlay != null,"Puste ustawienia.");
        var chatCommands=new[]{s.PointsCommand,s.RankingCommand,s.AdminCommand};
        Require(chatCommands.All(command=>command!=null&&Regex.IsMatch(command,@"\A![A-Za-z][A-Za-z0-9_-]{0,31}\z")),"Invalid chat command format. Use ! plus 1-32 ASCII letters, digits, _ or -, starting with a letter.");
        Require(chatCommands.Distinct(StringComparer.OrdinalIgnoreCase).Count()==chatCommands.Length,"Chat commands must be different (case-insensitive).");
        Require(new[]{"pl","en"}.Contains(s.PanelLanguage)&&new[]{"pl","en"}.Contains(s.OverlayLanguage),"Nieprawidłowy język interfejsu.");
        Require(new[]{"STRICT","WHITESPACE","ATTACHED"}.Contains(s.VoteParsing),"Nieprawidłowy tryb rozpoznawania głosów.");
        Require(s.ArchiveRetention!=null,"Brak zasad czyszczenia archiwów.");
        ValidateRange(s.ArchiveRetention.MaxCount,1,1000,"Maksymalna liczba archiwów");
        ValidateRange(s.ArchiveRetention.MaxAgeDays,1,36500,"Maksymalny wiek archiwów");
        ValidateRange(s.UserCooldown,0,3600,"Cooldown user"); ValidateRange(s.GlobalCooldown,0,3600,"Cooldown global");
        var o=s.Overlay; Require(new[]{"FULL","COMPACT","MINIMAL","NO_LEADERBOARD"}.Contains(o.Layout),"Layout.");
        Require(Regex.IsMatch(o.Accent ?? "",@"^#[0-9a-fA-F]{6}$"),"Kolor HEX."); ValidateRange(o.Scale,40,150,"Skala"); ValidateRange(o.X,0,1800,"X"); ValidateRange(o.Y,0,1000,"Y");
        ValidateRange(o.Top,1,10,"TOP"); ValidateRange(o.Opacity,10,100,"Opacity"); ValidateRange(o.Volume,0,100,"Głośność");
        var actions=new[]{"start","lock","reveal","cancel","next","previous","pause","addtime","subtracttime","visibility","podium"};
        var bindings=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in s.Keys) { Require(actions.Contains(k.Key),"Nieznana akcja bindu."); ValidateText(k.Value,0,60,"Bind"); if(k.Value!="") { Require(Regex.IsMatch(k.Value,@"^(Ctrl\+)?(Alt\+)?(Shift\+)?(Key[A-Z]|Digit[0-9]|F([1-9]|1[0-2])|Space|Arrow(Left|Right|Up|Down))$"),"Format bindu."); Require(bindings.Add(k.Value),"Konflikt bindów."); } }
    }
    private void Validate(Root d) {
        Require(d != null && d.Schema == 2,"Nieobsługiwany State Schema.");
        Require(d.BaseScores != null && d.Scores != null && d.History != null && d.Bank != null && d.Sets != null && d.Templates != null && d.Teams != null && d.Members != null && d.BaseTeamScores != null && d.TeamScores != null && d.ImportBackup != null,"Niepełny stan V2.");
        ValidateRange(d.Revision,0,Int32.MaxValue-1,"Revision"); ValidateRange(d.Epoch,0,Int32.MaxValue-1,"Epoch"); ValidateText(d.Migration,0,120,"Migration");
        Require(d.BaseScores.Count<=25000 && d.Scores.Count<=25000 && d.ImportBackup.Count<=25000,"Za duży ranking.");
        foreach(var x in d.BaseScores) ValidateScore(x.Value,x.Key); foreach(var x in d.Scores) ValidateScore(x.Value,x.Key); foreach(var x in d.ImportBackup) ValidateScore(x.Value,x.Key);
        Require(d.History.Count<=MaxHistory && d.Bank.Count<=1000 && d.Sets.Count<=100 && d.Templates.Count<=100 && d.Teams.Count<=50 && d.Members.Count<=25000,"Limit rekordów.");
        var ids=new HashSet<string>(); foreach(var r in d.History) { ValidateRound(r); Require(ids.Add(r.Id),"Duplikat Round ID."); ValidateRange(r.Epoch,0,d.Epoch,"Round epoch"); }
        if(d.Current!=null) ValidateRound(d.Current);
        ids.Clear(); foreach(var q in d.Bank) { ValidateQuestion(q); Require(ids.Add(q.Id),"Duplikat Question ID."); }
        ids.Clear(); foreach(var q in d.Templates) { ValidateQuestion(q); Require(ids.Add(q.Id),"Duplikat Template ID."); }
        ids.Clear(); foreach(var set in d.Sets) { Require(set!=null && set.Questions!=null,"Pusty zestaw."); Identifier(set.Id); ValidateText(set.Name,1,80,"Set name"); Require(ids.Add(set.Id) && set.Questions.Count<=200,"Duplikat/limit zestawu."); foreach(var q in set.Questions) ValidateQuestion(q); }
        ids.Clear(); foreach(var team in d.Teams) { Require(team!=null,"Pusta drużyna."); Identifier(team.Id); ValidateText(team.Name,1,80,"Team name"); Require(ids.Add(team.Id),"Duplikat drużyny."); }
        foreach(var m in d.Members) { UserId(m.Key); Require(ids.Contains(m.Value),"Nieznana drużyna członka."); }
        foreach(var t in d.TeamScores) { Identifier(t.Key); ValidateRange(t.Value,0,250000000000L,"Team score"); }
        foreach(var t in d.BaseTeamScores) { Identifier(t.Key); ValidateRange(t.Value,0,250000000000L,"Base team score"); }
        if(d.Draft!=null) ValidateQuestion(d.Draft,true);
        ValidateCompetitive(d.Competitive); ValidateRange(d.PreparedQueueIndex,-1,d.Queue==null?0:d.Queue.Count,"Prepared queue index");
        foreach(var round in d.History.Concat(d.Current==null?new Round[0]:new[]{d.Current}))if(round.Competitive!=null&&round.Competitive.SeasonId!="")Require(round.Competitive.Settings.SeasonsEnabled&&d.Competitive.Seasons.Any(x=>x.Id==round.Competitive.SeasonId),"Round references an unknown season.");
        ValidateSettings(d.Settings); Identifier(d.QueueSetId,true); ValidateRange(d.QueueIndex,-1,d.Queue==null?0:d.Queue.Count,"Queue index");
        if(d.Queue!=null) { Require(d.Queue.Count<=200,"Queue limit"); foreach(var q in d.Queue) ValidateQuestion(q); }
    }
    private bool AddLegacyTemplateToBank(List<Question> bank, Question source) {
        ValidateQuestion(source); var q=Copy(source); int same=bank.FindIndex(x=>x.Id==q.Id);
        if(same<0) { bank.Add(q); return true; }
        if(JsonConvert.SerializeObject(bank[same])==JsonConvert.SerializeObject(q)) return false;
        q.Id=Id(); string baseName=String.IsNullOrWhiteSpace(q.Name)?q.Text:q.Name; const string suffix=" (legacy)";
        if(baseName.Length>80-suffix.Length)baseName=baseName.Substring(0,80-suffix.Length);q.Name=baseName+suffix;bank.Add(q);return true;
    }
    private bool ConsolidateLegacyTemplates(Root d) {
        if(d==null||d.Templates==null||d.Templates.Count==0)return false; bool changed=false;
        foreach(var q in d.Templates)changed=AddLegacyTemplateToBank(d.Bank,q)||changed;
        d.Templates.Clear(); string marker="templates -> Szablony"; string migration=(d.Migration??"").Trim();
        if(!migration.Contains(marker)){string next=migration==""?marker:migration+" - "+marker;d.Migration=next.Length<=120?next:next.Substring(0,120);}d.Revision++;return true;
    }
    private Root LoadState() {
        string raw=CPH.GetGlobalVar<string>(DataVariable,true);
        if(quizData!=null&&!String.IsNullOrWhiteSpace(raw)&&String.Equals(raw,storedStateJson,StringComparison.Ordinal))return quizData;
        storedStateJson=raw;
        if(!String.IsNullOrWhiteSpace(raw)) {
            var d=Read<Root>(raw);bool retiredFields=legacyCountersRemoved;
            bool changed=ConsolidateLegacyTemplates(d);
            string renamed=(d.Migration??"").Replace("Szablony"+" CINEMA","Szablony");
            if(retiredFields||renamed!=d.Migration){d.Migration=renamed;d.Revision++;changed=true;}
            Validate(d);if(changed){quizData=d;if(retiredFields)Snapshot("before LOCAL3 score migration",raw);SaveState();}return d;
        }
        var result=new Root();
        string scores=CPH.GetGlobalVar<string>("localQuiz_scores_v1",true);
        string backup=CPH.GetGlobalVar<string>("localQuiz_scores_before_import_v1",true);
        string state=CPH.GetGlobalVar<string>("localQuiz_state_v1",false);
        string names=CPH.GetGlobalVar<string>("localQuiz_voterNames_v1",false);
        if(!String.IsNullOrWhiteSpace(scores)) result.BaseScores=Read<Dictionary<string,ScoreEntry>>(scores);
        if(!String.IsNullOrWhiteSpace(backup)) {result.ImportBackup=Read<Dictionary<string,ScoreEntry>>(backup);result.HasImportBackup=true;}
        foreach(var kv in result.BaseScores) { Require(kv.Value!=null,"Uszkodzony ranking v1."); if(String.IsNullOrEmpty(kv.Value.UserId)) kv.Value.UserId=kv.Key; }
        foreach(var kv in result.ImportBackup) { Require(kv.Value!=null,"Uszkodzona kopia CSV v1."); if(String.IsNullOrEmpty(kv.Value.UserId)) kv.Value.UserId=kv.Key; }
        if(!String.IsNullOrWhiteSpace(state)) {
            LegacyState old=Read<LegacyState>(state);
            Require(old!=null && new[]{"idle","open","locked","revealed"}.Contains(old.Status),"Uszkodzony stan v1.");
            if(old.Status!="idle") {
                var q=new Question { Text=old.Question, Duration=0, FullPoints=old.PointsPerCorrect, PartialPoints=old.PointsPerAdjacent, Scoring=old.ScoringMode??"adjacent" };
                Require(old.Options!=null && old.Votes!=null,"Niepełna runda v1.");
                q.Options=old.Options.Select((x,i)=>new Option {Id="legacy-"+(i+1),Text=x.Text,Correct=i+1==old.CorrectIndex,Partial=old.PartialIndices!=null&&old.PartialIndices.Contains(i+1)}).ToList();
                var r=new Round {Id=String.IsNullOrEmpty(old.RoundId)?Id():old.RoundId,Question=q,Status=old.Status,Visible=old.Visible,StartedAt=old.StartedAt>0?old.StartedAt:Now(),ClosesAt=old.ClosesAt,ClockAt=Now(),Epoch=0,Legacy=true};
                var ns=String.IsNullOrWhiteSpace(names)?new Dictionary<string,string>():Read<Dictionary<string,string>>(names);
                foreach(var v in old.Votes) { Require(v.Value>=1&&v.Value<=q.Options.Count,"Nieprawidłowy głos v1."); r.Votes[v.Key]=new Vote {OptionId=q.Options[v.Value-1].Id,Name=ns.ContainsKey(v.Key)?ns[v.Key]:v.Key}; }
                if(r.Status=="revealed") { r.EndedAt=Now(); result.History.Add(Copy(r)); }
                result.Current=r;
            }
        }
        result.Scores=Copy(result.BaseScores); result.Migration="v1 -> schema 2 verified";
        Validate(result);
        CPH.SetGlobalVar("localQuiz_migration_backup_v1",JsonConvert.SerializeObject(new {schema=1,scores=scores??"",importBackup=backup??"",state=state??"",names=names??"",at=Now()}),true);
        var verified=Read<Root>(JsonConvert.SerializeObject(result)); Validate(verified);
        quizData=verified; Snapshot("migration v1 -> v2"); SaveState(); return quizData;
    }
    private void SaveState(bool recoveryCancellation = false) {
        Validate(quizData); string raw=JsonConvert.SerializeObject(quizData);
        Require(Encoding.UTF8.GetByteCount(raw)<=(recoveryCancellation?MaxBytes:MaxBytes-65536),"Stan przekroczył 8 MiB. Zmniejsz bank lub historię. Operacja nie została zapisana.");
        Require(String.Equals(CPH.GetGlobalVar<string>(DataVariable,true),storedStateJson,StringComparison.Ordinal),"Stan zmienił się w innej instancji. Odśwież panel i ponów operację.");
        CPH.SetGlobalVar(DataVariable,raw,true);
        Require(String.Equals(CPH.GetGlobalVar<string>(DataVariable,true),raw,StringComparison.Ordinal),"Nie udało się potwierdzić zapisu stanu.");
        storedStateJson=raw;
    }
    private void Snapshot(string reason,string sourceRaw=null) {
        var list=LoadSnapshots(); string body=sourceRaw??JsonConvert.SerializeObject(quizData);Require(Encoding.UTF8.GetByteCount(body)<=MaxBytes,"Stan jest za duży na snapshot. Dane źródłowe pozostają nienaruszone.");
        list.Add(new SnapshotEntry {Id=Id(),At=Now(),Reason=reason,Data=body});
        while(list.Count>5 || Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(list))>MaxSnapshotBytes) list.RemoveAt(0);
        CPH.SetGlobalVar(SnapshotVariable,JsonConvert.SerializeObject(list),true);
    }
    private List<SnapshotEntry> LoadSnapshots() {
        string s=CPH.GetGlobalVar<string>(SnapshotVariable,true);
        if(String.IsNullOrWhiteSpace(s)) return new List<SnapshotEntry>();
        Require(Encoding.UTF8.GetByteCount(s)<=MaxSnapshotBytes,"Uszkodzony/zbyt duży ring snapshotów.");
        var list=Read<List<SnapshotEntry>>(s,MaxSnapshotBytes);
        Require(list!=null&&list.Count<=5,"Uszkodzony ring snapshotów.");
        foreach(var x in list) { Require(x!=null,"Pusty snapshot."); ValidateText(x.Id,1,80,"Snapshot ID"); ValidateText(x.Reason,1,120,"Snapshot reason"); Require(x.Data!=null&&Encoding.UTF8.GetByteCount(x.Data)<=MaxBytes,"Snapshot size"); }
        return list;
    }
    private string RankingArchiveDirectory() {
        string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        Require(!String.IsNullOrWhiteSpace(local) && Path.IsPathRooted(local), "Brak lokalnego katalogu danych Windows. Reset nie został wykonany.");
        return Path.Combine(local, "LukiQuizEngine", "RankingArchives");
    }
    private string OpenRankingArchiveFolder() {
        string directory="";
        try {
            directory=SafeArchiveDirectory(false);
            Require(Environment.OSVersion.Platform==PlatformID.Win32NT,"Otwieranie folderu archiwów jest dostępne tylko w Windows.");
            string windows=Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            Require(!String.IsNullOrWhiteSpace(windows) && Path.IsPathRooted(windows),"Nie można ustalić katalogu Windows.");
            string explorer=Path.Combine(windows,"explorer.exe");
            Require(File.Exists(explorer) && directory.IndexOf('"')<0,"Eksplorator Windows jest niedostępny.");
            Directory.CreateDirectory(directory);
            CheckArchiveDirectoryPath(directory);
            var start=new System.Diagnostics.ProcessStartInfo {FileName=explorer,Arguments="\""+directory+"\"",UseShellExecute=true};
            using(var process=System.Diagnostics.Process.Start(start)) {Require(process!=null,"Eksplorator Windows nie potwierdził uruchomienia.");}
            return directory;
        } catch(Exception ex) {
            CPH.LogWarn("[LukiQuiz V2] Otwieranie folderu archiwów: "+ex.GetType().Name);
            throw new InvalidOperationException("Nie udało się otworzyć folderu archiwów"+(directory==""?"":": "+directory)+". "+(ex is InvalidOperationException?ex.Message:"Sprawdź dostęp do folderu i Eksploratora Windows."));
        }
    }
    private string ArchiveHash(string value) {
        using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "").ToLowerInvariant();
    }
    private void VerifyArchiveFile(string path, string expected) {
        CheckArchiveFilePath(path);
        byte[] bytes=File.ReadAllBytes(path);
        Require(bytes.SequenceEqual(new UTF8Encoding(false,true).GetBytes(expected)), "Niepełny lub zmieniony plik archiwizacji.");
        Require(JToken.DeepEquals(JToken.Parse(new UTF8Encoding(false,true).GetString(bytes)),JToken.Parse(expected)), "Nieprawidłowy JSON archiwizacji.");
    }
    private void WriteArchiveFile(string path, string json) {
        JToken.Parse(json);
        CheckArchiveFilePath(path);
        string temporary=path+"."+Id()+".tmp";
        try {
            byte[] bytes=new UTF8Encoding(false,true).GetBytes(json);
            CheckArchiveFilePath(temporary);
            using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                stream.Write(bytes,0,bytes.Length);stream.Flush(true);
            }
            VerifyArchiveFile(temporary,json);
            CheckArchiveFilePath(temporary);CheckArchiveFilePath(path);
            File.Move(temporary,path);
            VerifyArchiveFile(path,json);
        } finally {
            try {CheckArchiveFilePath(temporary);if(File.Exists(temporary))File.Delete(temporary);}catch { }
        }
    }
    private RankingArchiveDocument ReadRankingArchive(string path) {
        CheckArchiveFilePath(path);
        Require(new FileInfo(path).Length<=MaxBytes, "Zbyt duże archiwum. Reset nie został wykonany.");
        var archive=Read<RankingArchiveDocument>(File.ReadAllText(path,new UTF8Encoding(false,true)));
        Require(archive!=null && archive.Format=="LukiQuizRankingArchive" && archive.Schema==1 && archive.Scores!=null && archive.Teams!=null && archive.Members!=null && archive.TeamScores!=null && archive.BaseTeamScores!=null,"Niepełne archiwum. Reset nie został wykonany.");
        Require(archive.Users==archive.Scores.Count && archive.ArchiveId==Path.GetFileNameWithoutExtension(path),"Niepoprawna liczba wyników lub ID archiwum.");
        ValidateText(archive.AppVersion,1,30,"Archive AppVersion");Identifier(archive.Build);Identifier(archive.EnginePatch);
        foreach(var score in archive.Scores)ValidateScore(score.Value,score.Key);
        if(archive.ContentHash!=null)ValidateArchiveContentHash(archive);
        return archive;
    }
    private ArchiveResetResult ArchiveRankingBeforeReset(string directory, string sourceHash) {
        if(quizData.Scores.Count==0 && !quizData.TeamScores.Values.Any(x=>x!=0) && !quizData.BaseTeamScores.Values.Any(x=>x!=0))
            return new ArchiveResetResult {status="empty",path=directory};
        var expected=new RankingArchiveDocument {
            AppVersion=AppVersion,Build=BuildLabel,EnginePatch=EnginePatch,SourceStateHash=sourceHash,
            Epoch=quizData.Epoch,Revision=quizData.Revision,Users=quizData.Scores.Count,
            Scores=quizData.Scores,Teams=quizData.Teams,Members=quizData.Members,
            TeamMode=quizData.Settings.TeamMode,TeamScores=quizData.TeamScores,BaseTeamScores=quizData.BaseTeamScores
        };
        var matches=Directory.GetFiles(directory,"ranking-*-"+sourceHash+".json");
        Require(matches.Length<=1,"Niejednoznaczne archiwum źródłowego stanu. Reset nie został wykonany.");
        string path;
        bool reused=matches.Length==1;
        if(reused) {
            path=SafeArchivePath(directory,Path.GetFileNameWithoutExtension(matches[0]));var existing=ReadRankingArchive(path);
            expected.ArchiveId=existing.ArchiveId;expected.CreatedAt=existing.CreatedAt;expected.CreatedAtUtc=existing.CreatedAtUtc;
            expected.AppVersion=existing.AppVersion;expected.Build=existing.Build;expected.EnginePatch=existing.EnginePatch;
            if(existing.ContentHash!=null)expected.ContentHash=ArchiveHash(JsonConvert.SerializeObject(expected));
            Require(JToken.DeepEquals(JToken.FromObject(existing),JToken.FromObject(expected)),"Istniejące archiwum nie zawiera kompletnych wyników. Reset nie został wykonany.");
        } else {
            var utc=DateTime.UtcNow;
            expected.CreatedAt=Now();expected.CreatedAtUtc=utc.ToString("o",System.Globalization.CultureInfo.InvariantCulture);
            expected.ArchiveId="ranking-"+utc.ToString("yyyy-MM-dd_HH-mm-ss-fff",System.Globalization.CultureInfo.InvariantCulture)+"-"+sourceHash;
            expected.ContentHash=ArchiveHash(JsonConvert.SerializeObject(expected));
            path=SafeArchivePath(directory,expected.ArchiveId);
            string json=JsonConvert.SerializeObject(expected);
            Require(Encoding.UTF8.GetByteCount(json)<=MaxBytes,"Ranking jest za duży na archiwum. Reset nie został wykonany.");
            WriteArchiveFile(path,json);
            var verified=ReadRankingArchive(path);
            Require(JToken.DeepEquals(JToken.FromObject(verified),JToken.FromObject(expected)),"Weryfikacja pełnego archiwum nie powiodła się.");
        }
        return new ArchiveResetResult {status="archived",path=path,archiveId=expected.ArchiveId,users=expected.Users,reused=reused};
    }
    private ResetArchiveIntent ReadResetIntent(string path, string directory) {
        CheckArchiveFilePath(path);
        Require(new FileInfo(path).Length<=65536,"Uszkodzone potwierdzenie resetu.");
        var intent=Read<ResetArchiveIntent>(File.ReadAllText(path,new UTF8Encoding(false,true)),65536);
        Require(intent!=null && intent.Schema==1 && intent.RequestId==requestId && intent.Archive!=null && Regex.IsMatch(intent.SourceHash??"",@"^[a-f0-9]{64}$") && Regex.IsMatch(intent.TargetHash??"",@"^[a-f0-9]{64}$"),"Nieprawidłowe potwierdzenie resetu.");
        Require(intent.Archive.status=="empty" || intent.Archive.status=="archived","Nieprawidłowy status archiwizacji.");
        string expected=intent.Archive.status=="empty" ? directory : SafeArchivePath(directory,intent.Archive.archiveId);
        Require(String.Equals(expected,intent.Archive.path,StringComparison.Ordinal),"Nieprawidłowa ścieżka potwierdzenia resetu.");
        if(intent.Archive.status=="archived") {
            var archive=ReadRankingArchive(expected);
            Require(archive.SourceStateHash==intent.SourceHash && archive.Users==intent.Archive.users,"Archiwum nie zgadza się z potwierdzeniem resetu.");
            Require(ArchiveHash(File.ReadAllText(expected,new UTF8Encoding(false,true)))==intent.ArchiveHash,"Plik archiwum został zmieniony. Nie można potwierdzić resetu.");
        }
        return intent;
    }
    private void ResetScoresWithArchive() {
        Confirm("RESET");
        string directory;
        try {directory=SafeArchiveDirectory(true);}
        catch(Exception ex) {throw new InvalidOperationException("Nie można otworzyć lokalnego katalogu archiwów. Reset nie został wykonany. "+SafeError(ex));}
        string lockPath=Path.Combine(directory,".reset.lock");CheckArchiveFilePath(lockPath);
        using(var resetLock=new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) {
            CheckArchiveFilePath(lockPath);
            string sourceHash=ArchiveHash(storedStateJson);
            string intentPath=requestId=="" ? null : Path.Combine(directory,".resets","reset-"+ArchiveHash(requestId)+".json");
            CheckArchiveDirectoryPath(Path.Combine(directory,".resets"));
            if(intentPath!=null) {CheckArchiveFilePath(intentPath);CheckArchiveFilePath(intentPath+".committed.json");}
            ResetArchiveIntent intent=null;
            if(intentPath!=null && File.Exists(intentPath)) {
                intent=ReadResetIntent(intentPath,directory);
                string committed=intentPath+".committed.json";
                if(File.Exists(committed)) {
                    VerifyArchiveFile(committed,JsonConvert.SerializeObject(intent));
                    Require(sourceHash!=intent.SourceHash && quizData.Epoch>=intent.TargetEpoch && quizData.Revision>=intent.TargetRevision,"Zapisano potwierdzenie wcześniejszego resetu, ale obecny stan odpowiada starszemu rankingowi. Nie wykonano kolejnego resetu. Sprawdź ranking i archiwum po przywróceniu danych lub awarii Streamer.bot.");
                    resetArchive=intent.Archive;resetArchive.reused=true;resetArchive.replayed=true;resetResultRevision=intent.TargetRevision;resetStateSaved=true;
                    BroadcastScores();BroadcastData();return;
                }
                if(sourceHash==intent.TargetHash) {
                    WriteArchiveFile(committed,JsonConvert.SerializeObject(intent));
                    resetArchive=intent.Archive;resetArchive.reused=true;resetArchive.replayed=true;resetResultRevision=intent.TargetRevision;resetStateSaved=true;
                    BroadcastScores();BroadcastData();return;
                }
                Require(sourceHash==intent.SourceHash,"Nie można rozstrzygnąć poprzedniego resetu po zmianie stanu. Ranking pozostaje bez zmian. Odśwież panel i sprawdź archiwum.");
            }
            EnsureRoundFinished();
            Require(String.Equals(CPH.GetGlobalVar<string>(DataVariable,true),storedStateJson,StringComparison.Ordinal),"Stan zmienił się przed archiwizacją. Odśwież panel. Reset nie został wykonany.");
            try {resetArchive=ArchiveRankingBeforeReset(directory,sourceHash);}
            catch(Exception ex) {throw new InvalidOperationException("Nie udało się zapisać i zweryfikować archiwum. Reset nie został wykonany. Sprawdź dostęp do folderu "+directory+". "+SafeError(ex));}
            if(intent==null)Snapshot("reset ranking");NewEpoch(new Dictionary<string,ScoreEntry>());
            quizData.TeamScores.Clear();quizData.BaseTeamScores.Clear();quizData.ImportBackup.Clear();quizData.HasImportBackup=false;
            quizData.Revision++;
            var nextIntent=new ResetArchiveIntent {RequestId=requestId,SourceHash=sourceHash,TargetHash=ArchiveHash(JsonConvert.SerializeObject(quizData)),TargetRevision=quizData.Revision,TargetEpoch=quizData.Epoch,Archive=resetArchive,ArchiveHash=resetArchive.status=="archived"?ArchiveHash(File.ReadAllText(resetArchive.path,new UTF8Encoding(false,true))):""};
            if(intentPath!=null) {
                Directory.CreateDirectory(Path.GetDirectoryName(intentPath));
                CheckArchiveDirectoryPath(Path.GetDirectoryName(intentPath));
                if(intent==null)WriteArchiveFile(intentPath,JsonConvert.SerializeObject(nextIntent));
                else Require(intent.SourceHash==nextIntent.SourceHash && intent.TargetHash==nextIntent.TargetHash,"Wynik ponowionego resetu jest niejednoznaczny. Reset nie został zapisany.");
            }
            SaveState();resetStateSaved=true;
            if(intentPath!=null)WriteArchiveFile(intentPath+".committed.json",JsonConvert.SerializeObject(intent??nextIntent));
            resetResultRevision=quizData.Revision;
            BroadcastState();BroadcastScores();BroadcastData();
            CPH.LogInfo("[LukiQuiz V2] RESET rankingu zapisany, revision="+quizData.Revision+", scores="+quizData.Scores.Count);
            TryAutomaticArchiveCleanup(directory,resetArchive==null?"":resetArchive.archiveId);
        }
    }
    public class ArchiveResetResult {
        public string status {get;set;}="empty";
        public string path {get;set;}="";
        public string archiveId {get;set;}="";
        public int users {get;set;}
        public bool reused {get;set;}
        public bool replayed {get;set;}
    }
    public class RankingArchiveDocument {
        public string Format {get;set;}="LukiQuizRankingArchive";
        public int Schema {get;set;}=1;
        public string ArchiveId {get;set;}
        public long CreatedAt {get;set;}
        public string CreatedAtUtc {get;set;}
        public string AppVersion {get;set;}
        public string Build {get;set;}
        public string EnginePatch {get;set;}
        public string SourceStateHash {get;set;}
        public int Epoch {get;set;}
        public int Revision {get;set;}
        public int Users {get;set;}
        public Dictionary<string,ScoreEntry> Scores {get;set;}
        public bool TeamMode {get;set;}
        public List<Team> Teams {get;set;}
        public Dictionary<string,string> Members {get;set;}
        public Dictionary<string,long> TeamScores {get;set;}
        public Dictionary<string,long> BaseTeamScores {get;set;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public string Name {get;set;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public string ArchiveType {get;set;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public Dictionary<string,ScoreEntry> BaseScores {get;set;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public long? TotalPoints {get;set;}
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] public string ContentHash {get;set;}
    }
    public class ArchiveSaveResult {
        public string status {get;set;}="archived";
        public string archiveId {get;set;}
        public string name {get;set;}
        public int users {get;set;}
        public bool replayed {get;set;}
    }
    public class ArchiveRestoreResult {
        public string status {get;set;}
        public string archiveId {get;set;}="";
        public string name {get;set;}="";
        public int users {get;set;}
        public string backupArchiveId {get;set;}="";
        public bool replayed {get;set;}
    }
    public class ArchiveMetadata {
        public string archiveId {get;set;}
        public string name {get;set;}
        public long createdAt {get;set;}
        public string createdAtUtc {get;set;}
        public string type {get;set;}
        public int users {get;set;}
        public long? totalPoints {get;set;}
        public string appVersion {get;set;}
        public long sizeBytes {get;set;}
    }
    public class ArchiveCleanupRequest {
        public string mode {get;set;}="manual";
        public List<string> archiveIds {get;set;}=new List<string>();
    }
    public class ArchiveCleanupItem {
        public string archiveId {get;set;}
        public string archiveHash {get;set;}
        public string name {get;set;}
        public string type {get;set;}
        public long sizeBytes {get;set;}
        public long createdAt {get;set;}
    }
    public class ArchiveCleanupPlan {
        public string planId {get;set;}
        public string planHash {get;set;}
        public long expiresAt {get;set;}
        public List<ArchiveCleanupItem> items {get;set;}=new List<ArchiveCleanupItem>();
        public int protectedCount {get;set;}
        public int qualifiedCount {get;set;}
        public bool limited {get;set;}
    }
    public class ArchiveCleanupResult {
        public string planId {get;set;}
        public int deletedCount {get;set;}
        public long deletedBytes {get;set;}
        public int failedCount {get;set;}
        public bool automatic {get;set;}
    }
    public class RankingOperationIntent {
        public int Schema {get;set;}=1;
        public string RequestId {get;set;}
        public string Operation {get;set;}
        public string ArgumentsHash {get;set;}
        public string ArchiveId {get;set;}
        public string ArchiveHash {get;set;}
        public string ArchiveName {get;set;}
        public int Users {get;set;}
        public string BackupArchiveId {get;set;}="";
        public string BackupHash {get;set;}="";
        public string SourceHash {get;set;}
        public string TargetHash {get;set;}="";
        public int TargetEpoch {get;set;}
        public int TargetRevision {get;set;}
    }
    private Root ReadArchiveState() {
        string raw=CPH.GetGlobalVar<string>(DataVariable,true);
        Require(!String.IsNullOrWhiteSpace(raw),"Brak zapisanego stanu V2. Połącz panel i odśwież ranking.");
        var state=Read<Root>(raw);Validate(state);storedStateJson=raw;return state;
    }
    private int ReadArchiveStateRevision() {
        try {
            string raw=CPH.GetGlobalVar<string>(DataVariable,true);
            if(String.IsNullOrWhiteSpace(raw))return 0;
            var state=Read<Root>(raw);ValidateRange(state.Revision,0,Int32.MaxValue-1,"Revision");return state.Revision;
        }catch(Exception){return 0;}
    }
    private void CheckArchiveDirectoryPath(string directory) {
        for(var part=new DirectoryInfo(Path.GetFullPath(directory));part!=null;part=part.Parent) {
            FileAttributes attributes;
            try {attributes=File.GetAttributes(part.FullName);}
            catch(FileNotFoundException) {continue;}
            catch(DirectoryNotFoundException) {continue;}
            Require((attributes&FileAttributes.ReparsePoint)==0&&(attributes&FileAttributes.Directory)!=0,"Katalog archiwów zawiera dowiązanie lub nie jest katalogiem. Operacja została odrzucona.");
        }
    }
    private void CheckArchiveFilePath(string path) {
        string root=Path.GetFullPath(RankingArchiveDirectory()).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
        string full=Path.GetFullPath(path);
        Require(full.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase),"Plik jest poza dozwolonym katalogiem archiwów.");
        CheckArchiveDirectoryPath(Path.GetDirectoryName(full));
        FileAttributes attributes;
        try {attributes=File.GetAttributes(full);}
        catch(FileNotFoundException) {return;}
        catch(DirectoryNotFoundException) {return;}
        Require((attributes&(FileAttributes.ReparsePoint|FileAttributes.Directory))==0,"Plik archiwizacji jest dowiązaniem lub katalogiem. Operacja została odrzucona.");
    }
    private string SafeArchiveDirectory(bool create) {
        string directory=RankingArchiveDirectory();CheckArchiveDirectoryPath(directory);
        if(create)Directory.CreateDirectory(directory);
        CheckArchiveDirectoryPath(directory);return directory;
    }
    private bool IsRankingArchiveId(string value) {
        if(!Regex.IsMatch(value??"",@"^(ranking|manual|safety)-[0-9]{4}-[0-9]{2}-[0-9]{2}_[0-9]{2}-[0-9]{2}-[0-9]{2}-[0-9]{3}-[a-f0-9]{32}([a-f0-9]{32})?$"))return false;
        DateTime date;return DateTime.TryParseExact(value.Substring(value.IndexOf('-')+1,23),"yyyy-MM-dd_HH-mm-ss-fff",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.None,out date);
    }
    private string SafeArchivePath(string directory,string archiveId) {
        Require(IsRankingArchiveId(archiveId),"Nieprawidłowy identyfikator archiwum.");
        CheckArchiveDirectoryPath(directory);
        string path=Path.GetFullPath(Path.Combine(directory,archiveId+".json"));
        Require(String.Equals(Path.GetDirectoryName(path),Path.GetFullPath(directory),StringComparison.OrdinalIgnoreCase),"Archiwum jest poza dozwolonym katalogiem.");
        CheckArchiveFilePath(path);
        return path;
    }
    private string ArchiveType(RankingArchiveDocument archive) {return archive.ArchiveType??"automatic";}
    private void ValidateArchiveContentHash(RankingArchiveDocument archive) {
        string expected=archive.ContentHash;Require(Regex.IsMatch(expected??"",@"^[a-f0-9]{64}$"),"Nieprawidłowy hash archiwum.");
        try {archive.ContentHash=null;Require(ArchiveHash(JsonConvert.SerializeObject(archive))==expected,"Archiwum nie przeszło kontroli integralności.");}finally{archive.ContentHash=expected;}
    }
    private ArchiveMetadata ArchiveInfo(RankingArchiveDocument archive) {
        return new ArchiveMetadata {archiveId=archive.ArchiveId,name=String.IsNullOrWhiteSpace(archive.Name)?new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc).AddMilliseconds(archive.CreatedAt).ToString("yyyy-MM-dd HH:mm:ss 'UTC'",System.Globalization.CultureInfo.InvariantCulture):archive.Name,createdAt=archive.CreatedAt,createdAtUtc=archive.CreatedAtUtc,type=ArchiveType(archive),users=archive.Users,totalPoints=archive.Scores==null?archive.TotalPoints:archive.Scores.Values.Sum(x=>(long)x.Points),appVersion=archive.AppVersion};
    }
    private void ValidateRankingArchive(RankingArchiveDocument archive,string path) {
        Require(archive!=null&&archive.Format=="LukiQuizRankingArchive"&&archive.Schema==1&&archive.Scores!=null&&archive.Teams!=null&&archive.Members!=null&&archive.TeamScores!=null&&archive.BaseTeamScores!=null,"Niepełne archiwum rankingu.");
        Require(IsRankingArchiveId(archive.ArchiveId)&&archive.ArchiveId==Path.GetFileNameWithoutExtension(path)&&archive.Users==archive.Scores.Count,"Niepoprawne ID lub liczba wyników archiwum.");
        Require(new[]{"2.0.0","2.0.1","2.0.2","2.1.0","2.2.0"}.Contains(archive.AppVersion)&&new[]{"FC1","STYLE4","PHoeniX","QUPA"}.Contains(archive.Build)&&new[]{"FC1","STYLE4","PHoeniX","QUPA"}.Contains(archive.EnginePatch),"Nieobsługiwane oznaczenie wersji archiwum.");
        ValidateRange(archive.CreatedAt,1,4102444800000L,"Data archiwum");ValidateRange(archive.Epoch,0,Int32.MaxValue-1,"Archive epoch");ValidateRange(archive.Revision,0,Int32.MaxValue-1,"Archive revision");
        DateTime created;Require(DateTime.TryParse(archive.CreatedAtUtc,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind,out created)&&created.Kind==DateTimeKind.Utc&&Math.Abs((created-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalMilliseconds-archive.CreatedAt)<2000,"Nieprawidłowa data archiwum.");
        Require(Regex.IsMatch(archive.SourceStateHash??"",@"^[a-f0-9]{64}$"),"Nieprawidłowy SourceStateHash archiwum.");
        string type=ArchiveType(archive);Require(new[]{"automatic","manual","safety"}.Contains(type),"Nieznany typ archiwum.");
        Require(archive.ArchiveId.StartsWith(type=="automatic"?"ranking-":type+"-",StringComparison.Ordinal),"Typ archiwum nie zgadza się z ID.");
        if(type=="automatic")Require(archive.ArchiveId.EndsWith("-"+archive.SourceStateHash,StringComparison.Ordinal),"Niezgodny SourceStateHash archiwum.");
        if(archive.Name!=null){ValidateText(archive.Name,1,80,"Nazwa archiwum");Require(!archive.Name.Any(Char.IsControl),"Nazwa archiwum zawiera znaki sterujące.");}
        var state=new Root {Epoch=archive.Epoch,Revision=archive.Revision,Scores=archive.Scores,BaseScores=archive.BaseScores??archive.Scores,Teams=archive.Teams,Members=archive.Members,TeamScores=archive.TeamScores,BaseTeamScores=archive.BaseTeamScores};Validate(state);
        if(archive.TotalPoints.HasValue)Require(archive.TotalPoints.Value==archive.Scores.Values.Sum(x=>(long)x.Points),"Niezgodna suma punktów archiwum.");
        if(archive.ContentHash!=null) {
            ValidateArchiveContentHash(archive);
        } else Require(type=="automatic"&&archive.Name==null&&archive.BaseScores==null&&archive.TotalPoints==null,"Nowe archiwum nie zawiera kontroli integralności.");
    }
    private RankingArchiveDocument ReadManagedArchive(string directory,string archiveId,out string fileHash) {
        string path=SafeArchivePath(directory,archiveId);Require(File.Exists(path),"Plik archiwum nie istnieje.");
        Require(new FileInfo(path).Length>0&&new FileInfo(path).Length<=MaxBytes,"Archiwum jest puste lub przekracza 8 MiB.");
        string raw=File.ReadAllText(path,new UTF8Encoding(false,true));
        var archive=Read<RankingArchiveDocument>(raw);ValidateRankingArchive(archive,path);fileHash=ArchiveHash(raw);return archive;
    }
    private RankingArchiveDocument CreateManagedArchive(string directory,string type,string name,string archiveId=null) {
        var utc=DateTime.UtcNow;
        if(type=="manual")utc=utc.AddTicks(-(utc.Ticks%TimeSpan.TicksPerMillisecond));
        if(archiveId!=null) {
            Require(type=="manual"&&IsRankingArchiveId(archiveId)&&archiveId.StartsWith("manual-",StringComparison.Ordinal),"Nieprawidłowe ID przerwanego zapisu.");
            Require(DateTime.TryParseExact(archiveId.Substring(7,23),"yyyy-MM-dd_HH-mm-ss-fff",System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.AssumeUniversal|System.Globalization.DateTimeStyles.AdjustToUniversal,out utc),"Nieprawidłowa data przerwanego zapisu.");
        }
        var archive=new RankingArchiveDocument {ArchiveId=archiveId??type+"-"+utc.ToString("yyyy-MM-dd_HH-mm-ss-fff",System.Globalization.CultureInfo.InvariantCulture)+"-"+Id(),CreatedAt=type=="manual"?(long)(utc-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalMilliseconds:Now(),CreatedAtUtc=utc.ToString("o",System.Globalization.CultureInfo.InvariantCulture),AppVersion=AppVersion,Build=BuildLabel,EnginePatch=EnginePatch,SourceStateHash=ArchiveHash(storedStateJson),Epoch=quizData.Epoch,Revision=quizData.Revision,Users=quizData.Scores.Count,Scores=Copy(quizData.Scores),BaseScores=Copy(quizData.BaseScores),Teams=Copy(quizData.Teams),Members=Copy(quizData.Members),TeamMode=quizData.Settings.TeamMode,TeamScores=Copy(quizData.TeamScores),BaseTeamScores=Copy(quizData.BaseTeamScores),Name=name,ArchiveType=type};
        if(String.IsNullOrWhiteSpace(archive.Name))archive.Name=utc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'",System.Globalization.CultureInfo.InvariantCulture);
        archive.TotalPoints=archive.Scores.Values.Sum(x=>(long)x.Points);
        archive.ContentHash=ArchiveHash(JsonConvert.SerializeObject(archive));
        string json=JsonConvert.SerializeObject(archive);Require(Encoding.UTF8.GetByteCount(json)<=MaxBytes,"Ranking przekracza limit 8 MiB archiwum.");
        ValidateRankingArchive(archive,SafeArchivePath(directory,archive.ArchiveId));return archive;
    }
    private void PublishManagedArchive(string directory,RankingArchiveDocument archive) {
        string path=SafeArchivePath(directory,archive.ArchiveId),json=JsonConvert.SerializeObject(archive);WriteArchiveFile(path,json);
        string hash;ReadManagedArchive(directory,archive.ArchiveId,out hash);Require(hash==ArchiveHash(json),"Zapisane archiwum nie zgadza się ze źródłem.");
    }
    private string RankingOperationPath(string directory,string id,bool create) {
        Require(Regex.IsMatch(id??"",@"^[a-zA-Z0-9_-]{1,80}$"),"Nieprawidłowe ID operacji.");
        string journal=Path.Combine(directory,".ranking-operations");CheckArchiveDirectoryPath(journal);if(create)Directory.CreateDirectory(journal);CheckArchiveDirectoryPath(journal);
        string path=Path.Combine(journal,ArchiveHash(id)+".json");
        foreach(string file in new[]{path,path+".committed.json"})CheckArchiveFilePath(file);
        return path;
    }
    private RankingOperationIntent ReadRankingIntent(string path,string id) {
        CheckArchiveFilePath(path);
        Require(new FileInfo(path).Length<=65536,"Potwierdzenie operacji jest za duże.");
        var intent=Read<RankingOperationIntent>(File.ReadAllText(path,new UTF8Encoding(false,true)),65536);
        Require(intent!=null&&intent.Schema==1&&intent.RequestId==id&&new[]{"savearchive","restorearchive"}.Contains(intent.Operation)&&IsRankingArchiveId(intent.ArchiveId),"Nieprawidłowe potwierdzenie operacji.");
        foreach(string hash in new[]{intent.ArgumentsHash,intent.ArchiveHash,intent.SourceHash})Require(Regex.IsMatch(hash??"",@"^[a-f0-9]{64}$"),"Nieprawidłowa integralność potwierdzenia.");
        ValidateText(intent.ArchiveName,1,80,"Nazwa archiwum");ValidateRange(intent.Users,0,25000,"Liczba wyników");ValidateRange(intent.TargetRevision,0,Int32.MaxValue-1,"Revision operacji");ValidateRange(intent.TargetEpoch,0,Int32.MaxValue-1,"Epoch operacji");
        if(intent.Operation=="restorearchive")Require(IsRankingArchiveId(intent.BackupArchiveId)&&Regex.IsMatch(intent.BackupHash??"",@"^[a-f0-9]{64}$")&&Regex.IsMatch(intent.TargetHash??"",@"^[a-f0-9]{64}$"),"Niepełne potwierdzenie przywracania.");return intent;
    }
    private bool ArchiveTeamsCompatible(RankingArchiveDocument archive) {
        if(archive.TeamMode!=quizData.Settings.TeamMode)return false;
        if(archive.TeamScores.Count==0&&archive.BaseTeamScores.Count==0&&!archive.TeamMode)return true;
        return JToken.DeepEquals(JToken.FromObject(archive.Teams.OrderBy(x=>x.Id,StringComparer.Ordinal)),JToken.FromObject(quizData.Teams.OrderBy(x=>x.Id,StringComparer.Ordinal)))&&JToken.DeepEquals(JToken.FromObject(archive.Members.OrderBy(x=>x.Key,StringComparer.Ordinal)),JToken.FromObject(quizData.Members.OrderBy(x=>x.Key,StringComparer.Ordinal)));
    }
    private void HandleArchiveOperation(string op) {
        string[] allowed=op=="savearchive"?new[]{"archiveName"}:op=="listarchives"?new[]{"search","offset","limit","sort","archiveType"}:op=="previewarchive"?new[]{"archiveId","offset","limit"}:op=="statusarchive"?new[]{"operationId"}:op=="previewarchivecleanup"?new[]{"payloadJson"}:op=="deletearchives"?new[]{"planId","planHash","confirmText"}:new[]{"archiveId","archiveHash","expectedRevision","confirmText"};
        Require(requestArgs.Keys.All(allowed.Contains),"Nieznany argument archiwum rankingu.");
        if(op=="previewarchivecleanup"||op=="deletearchives")EnsureRoundFinished();
        string directory=SafeArchiveDirectory(op=="savearchive"||op=="restorearchive"||op=="previewarchivecleanup"||op=="deletearchives");
        if(op=="listarchives"){ListRankingArchives(directory);return;}
        if(op=="previewarchive") {
            string hash;var archive=ReadManagedArchive(directory,GetStringArg("archiveId"),out hash);int offset=GetIntArg("offset",0),limit=GetIntArg("limit",100);ValidateRange(offset,0,25000,"Offset");ValidateRange(limit,1,100,"Limit");
            bool teams=ArchiveTeamsCompatible(archive);bool finished=quizData.Current==null||quizData.Current.Status=="revealed"||quizData.Current.Status=="cancelled";
            Broadcast(new{type="quiz-archive-preview",appVersion=AppVersion,archive=ArchiveInfo(archive),archiveHash=hash,offset=offset,limit=limit,total=archive.Users,hasMore=offset+limit<archive.Users,scores=SortScores(archive.Scores).Skip(offset).Take(limit).Select((x,i)=>new{rank=offset+i+1,userId=x.UserId,userName=x.UserName,points=x.Points,correctAnswers=x.CorrectAnswers,answeredQuestions=x.AnsweredQuestions,lastUpdatedAt=x.LastUpdatedAt}).ToList(),restoreAllowed=teams&&finished,teamWarning=teams?"":"Definicje drużyn, przypisania widzów lub tryb drużynowy różnią się od archiwum. Przywracanie jest zablokowane."});return;
        }
        if(op=="statusarchive") {ResolveRankingRestore(directory,GetStringArg("operationId"),null);return;}
        string lockPath=Path.Combine(directory,".reset.lock");CheckArchiveFilePath(lockPath);
        using(var transaction=new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None)) {
            CheckArchiveFilePath(lockPath);
            Require(String.Equals(CPH.GetGlobalVar<string>(DataVariable,true),storedStateJson,StringComparison.Ordinal),"Stan zmienił się. Odśwież ranking i powtórz potwierdzenie.");
            if(op=="previewarchivecleanup")PreviewArchiveCleanup(directory);
            else if(op=="deletearchives")DeleteArchiveCleanupPlan(directory);
            else if(op=="savearchive"){SaveRankingArchive(directory);TryAutomaticArchiveCleanup(directory,archiveSave==null?"":archiveSave.archiveId);}
            else RestoreRankingArchive(directory);
        }
    }
    private void SaveRankingArchive(string directory) {
        string name=GetStringArg("archiveName").Trim();ValidateText(name,0,80,"Nazwa archiwum");Require(!name.Any(Char.IsControl),"Nazwa archiwum zawiera znaki sterujące.");
        string argsHash=ArchiveHash(name),path=RankingOperationPath(directory,requestId,true);
        if(File.Exists(path)) {
            var previous=ReadRankingIntent(path,requestId);Require(previous.Operation=="savearchive"&&previous.ArgumentsHash==argsHash,"ID żądania zostało już użyte dla innej operacji.");
            if(!File.Exists(SafeArchivePath(directory,previous.ArchiveId))) {
                Require(!File.Exists(ArchiveDeletedMarker(directory,previous.ArchiveId)),"Archiwum zostało świadomie usunięte. Nie odtworzono go przy ponowieniu starego żądania.");
                Require(ArchiveHash(storedStateJson)==previous.SourceHash&&quizData.Epoch==previous.TargetEpoch&&quizData.Revision==previous.TargetRevision&&quizData.Scores.Count==previous.Users,"Zapis archiwum został przerwany, a stan źródłowy zmienił się. Nie odtworzono wcześniejszego pliku. Wykonaj świadomie nowy zapis jako nowe żądanie z nowym ID.");
                var recovered=CreateManagedArchive(directory,"manual",previous.ArchiveName,previous.ArchiveId);
                Require(ArchiveHash(JsonConvert.SerializeObject(recovered))==previous.ArchiveHash,"Nie można dokładnie odtworzyć przerwanego zapisu ze starszej intencji. Nie zapisano archiwum. Wykonaj świadomie nowy zapis jako nowe żądanie z nowym ID.");
                Require(String.Equals(CPH.GetGlobalVar<string>(DataVariable,true),storedStateJson,StringComparison.Ordinal),"Stan zmienił się. Odśwież ranking i powtórz potwierdzenie.");
                PublishManagedArchive(directory,recovered);
            }
            string hash;var existing=ReadManagedArchive(directory,previous.ArchiveId,out hash);Require(hash==previous.ArchiveHash&&existing.SourceStateHash==previous.SourceHash&&existing.Users==previous.Users&&ArchiveInfo(existing).name==previous.ArchiveName&&ArchiveType(existing)=="manual","Nie można potwierdzić wcześniejszego zapisu. Archiwum zmieniło się.");
            WriteArchiveFile(path+".committed.json",JsonConvert.SerializeObject(previous));
            archiveSave=new ArchiveSaveResult{archiveId=existing.ArchiveId,name=ArchiveInfo(existing).name,users=existing.Users,replayed=true};return;
        }
        var archive=CreateManagedArchive(directory,"manual",name);string json=JsonConvert.SerializeObject(archive);
        var intent=new RankingOperationIntent{RequestId=requestId,Operation="savearchive",ArgumentsHash=argsHash,ArchiveId=archive.ArchiveId,ArchiveHash=ArchiveHash(json),ArchiveName=archive.Name,Users=archive.Users,SourceHash=archive.SourceStateHash,TargetEpoch=quizData.Epoch,TargetRevision=quizData.Revision};
        WriteArchiveFile(path,JsonConvert.SerializeObject(intent));PublishManagedArchive(directory,archive);WriteArchiveFile(path+".committed.json",JsonConvert.SerializeObject(intent));
        archiveSave=new ArchiveSaveResult{archiveId=archive.ArchiveId,name=archive.Name,users=archive.Users};
    }
    private ArchiveRestoreResult RestoreResult(RankingOperationIntent intent,string status) {
        return new ArchiveRestoreResult{status=status,archiveId=intent.ArchiveId,name=intent.ArchiveName,users=intent.Users,backupArchiveId=intent.BackupArchiveId,replayed=true};
    }
    private void ResolveRankingRestore(string directory,string operationId,string argumentsHash) {
        string path=RankingOperationPath(directory,operationId,false);
        if(!File.Exists(path)){restoreArchive=new ArchiveRestoreResult{status="not-found",replayed=true};return;}
        var intent=ReadRankingIntent(path,operationId);Require(intent.Operation=="restorearchive"&&(argumentsHash==null||intent.ArgumentsHash==argumentsHash),"ID żądania zostało już użyte dla innej operacji.");
        string targetHash,backupHash;var target=ReadManagedArchive(directory,intent.ArchiveId,out targetHash);var backup=ReadManagedArchive(directory,intent.BackupArchiveId,out backupHash);
        Require(targetHash==intent.ArchiveHash&&backupHash==intent.BackupHash&&backup.SourceStateHash==intent.SourceHash&&ArchiveType(backup)=="safety"&&backup.Epoch+1==intent.TargetEpoch&&backup.Revision+1==intent.TargetRevision&&target.Users==intent.Users&&ArchiveInfo(target).name==intent.ArchiveName,"Archiwum lub kopia bezpieczeństwa uległy zmianie. Wynik operacji jest niepewny.");
        string committed=path+".committed.json",current=ArchiveHash(storedStateJson);
        if(File.Exists(committed)) {
            VerifyArchiveFile(committed,JsonConvert.SerializeObject(intent));
            bool consistent=current==intent.TargetHash||(current!=intent.SourceHash&&quizData.Epoch>=intent.TargetEpoch&&quizData.Revision>=intent.TargetRevision);
            restoreArchive=RestoreResult(intent,consistent?"restored":"uncertain");if(consistent)archiveResultRevision=intent.TargetRevision;return;
        }
        if(current==intent.TargetHash) {restoreArchive=RestoreResult(intent,"restored");archiveResultRevision=intent.TargetRevision;return;}
        restoreArchive=RestoreResult(intent,current==intent.SourceHash?"not-applied":"uncertain");
    }
    private void RestoreRankingArchive(string directory) {
        Require(String.Equals(GetStringArg("confirmText"),"PRZYWROC",StringComparison.Ordinal),"Wymagane potwierdzenie: PRZYWROC");string id=GetStringArg("archiveId"),expectedHash=GetStringArg("archiveHash"),revisionText=GetStringArg("expectedRevision");
        Require(IsRankingArchiveId(id)&&Regex.IsMatch(expectedHash??"",@"^[a-f0-9]{64}$"),"Nieprawidłowe ID lub hash wybranego archiwum.");
        int expectedRevision;Require(Int32.TryParse(revisionText,System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out expectedRevision)&&expectedRevision>=0,"Nieprawidłowa rewizja potwierdzenia.");
        string argumentsHash=ArchiveHash(id+"\n"+expectedHash+"\n"+revisionText),path=RankingOperationPath(directory,requestId,true);
        if(File.Exists(path)){ResolveRankingRestore(directory,requestId,argumentsHash);Require(restoreArchive.status=="restored","Poprzednie przywracanie nie ma pewnego potwierdzenia. Nie wykonano ponownej operacji. Sprawdź ranking i użyj nowego potwierdzenia.");return;}
        EnsureRoundFinished();Require(expectedRevision==quizData.Revision,"Ranking zmienił się od podglądu. Odśwież ranking i ponownie potwierdź przywrócenie.");
        string hash;var archive=ReadManagedArchive(directory,id,out hash);Require(hash==expectedHash,"Archiwum zmieniło się od podglądu. Otwórz je ponownie.");
        Require(ArchiveTeamsCompatible(archive),"Definicje drużyn, przypisania widzów lub tryb drużynowy różnią się od archiwum. Przywracanie nie nadpisuje tych ustawień.");
        Require(quizData.Epoch<Int32.MaxValue-1&&quizData.Revision<Int32.MaxValue-2,"Wyczerpano zakres epoki lub rewizji.");
        string sourceRaw=storedStateJson,sourceHash=ArchiveHash(sourceRaw);
        var backup=CreateManagedArchive(directory,"safety","Kopia przed przywróceniem "+DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss",System.Globalization.CultureInfo.InvariantCulture));PublishManagedArchive(directory,backup);
        string backupHash;ReadManagedArchive(directory,backup.ArchiveId,out backupHash);
        Snapshot("before ranking archive restore",sourceRaw);
        Require(LoadSnapshots().Any(x=>x.Reason=="before ranking archive restore"&&String.Equals(x.Data,sourceRaw,StringComparison.Ordinal)),"Nie potwierdzono zapisu snapshotu. Ranking pozostaje bez zmian.");
        var source=quizData;quizData=Copy(source);
        quizData.TeamScores=Copy(archive.TeamScores);NewEpoch(Copy(archive.Scores));quizData.BaseTeamScores=Copy(archive.TeamScores);
        quizData.ImportBackup.Clear();quizData.HasImportBackup=false;quizData.Revision++;
        Validate(quizData);string targetRaw=JsonConvert.SerializeObject(quizData);
        var intent=new RankingOperationIntent{RequestId=requestId,Operation="restorearchive",ArgumentsHash=argumentsHash,ArchiveId=id,ArchiveHash=hash,ArchiveName=ArchiveInfo(archive).name,Users=archive.Users,BackupArchiveId=backup.ArchiveId,BackupHash=backupHash,SourceHash=sourceHash,TargetHash=ArchiveHash(targetRaw),TargetEpoch=quizData.Epoch,TargetRevision=quizData.Revision};
        try {WriteArchiveFile(path,JsonConvert.SerializeObject(intent));SaveState();WriteArchiveFile(path+".committed.json",JsonConvert.SerializeObject(intent));}
        catch {quizData=null;throw new InvalidOperationException("Nie potwierdzono wyniku przywracania. Nie ponawiaj operacji. Sprawdź jej status i odśwież ranking. Kopia bezpieczeństwa: "+backup.ArchiveId);}
        restoreArchive=RestoreResult(intent,"restored");restoreArchive.replayed=false;archiveResultRevision=quizData.Revision;
        BroadcastState();BroadcastScores();BroadcastData();
    }
    private void ListRankingArchives(string directory) {
        string search=GetStringArg("search").Trim();ValidateText(search,0,80,"Wyszukiwanie");int offset=GetIntArg("offset",0),limit=GetIntArg("limit",25);ValidateRange(offset,0,10000,"Offset");ValidateRange(limit,1,50,"Limit");
        string sort=GetStringArg("sort");if(sort=="")sort="date-desc";Require(new[]{"date-desc","date-asc","size-desc","size-asc"}.Contains(sort),"Nieprawidłowe sortowanie archiwów.");
        string archiveType=GetStringArg("archiveType");if(archiveType=="")archiveType="all";Require(new[]{"all","automatic","manual","safety"}.Contains(archiveType),"Nieprawidłowy filtr archiwów.");
        bool truncated;int skipped;var catalog=ScanArchiveMetadata(directory,out truncated,out skipped);
        var filtered=catalog.Where(x=>(archiveType=="all"||x.type==archiveType)&&x.name.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0);
        var sorted=(sort=="date-asc"?filtered.OrderBy(x=>x.createdAt):sort=="size-asc"?filtered.OrderBy(x=>x.sizeBytes):sort=="size-desc"?filtered.OrderByDescending(x=>x.sizeBytes):filtered.OrderByDescending(x=>x.createdAt)).ThenBy(x=>x.archiveId,StringComparer.Ordinal).ToList();
        Broadcast(new{type="quiz-archives",appVersion=AppVersion,items=sorted.Skip(offset).Take(limit).ToList(),offset=offset,limit=limit,total=sorted.Count,hasMore=offset+limit<sorted.Count,truncated=truncated,skipped=skipped,archiveCount=catalog.Count,totalBytes=catalog.Sum(x=>x.sizeBytes),sort=sort,archiveType=archiveType});
    }
    private List<ArchiveMetadata> ScanArchiveMetadata(string directory,out bool truncated,out int skipped) {
        var items=new List<ArchiveMetadata>();skipped=0;truncated=false;long bytes=0;int scanned=0;
        var watch=System.Diagnostics.Stopwatch.StartNew();
        if(!Directory.Exists(directory))return items;
        CheckArchiveDirectoryPath(directory);
        foreach(string path in Directory.EnumerateFiles(directory,"*.json",SearchOption.TopDirectoryOnly)) {
            string id=Path.GetFileNameWithoutExtension(path);if(!IsRankingArchiveId(id))continue;
            if(++scanned>10000||watch.ElapsedMilliseconds>=5000){truncated=true;break;}
            try {
                SafeArchivePath(directory,id);long size=new FileInfo(path).Length;if(size<=0||size>MaxBytes){skipped++;continue;}
                if(bytes+size>64L*1024*1024){truncated=true;break;}bytes+=size;
                var metadata=ReadArchiveMetadata(path);metadata.sizeBytes=size;items.Add(metadata);
            }catch(Exception){skipped++;}
        }
        return items;
    }
    private HashSet<string> PendingArchiveReferences(string directory) {
        var references=new HashSet<string>(StringComparer.Ordinal);int inspected=0;long readBytes=0;var watch=System.Diagnostics.Stopwatch.StartNew();
        foreach(string name in new[]{".resets",".ranking-operations"}) {
            string journal=Path.Combine(directory,name);CheckArchiveDirectoryPath(journal);if(!Directory.Exists(journal))continue;
            foreach(string path in Directory.EnumerateFiles(journal,"*.json",SearchOption.TopDirectoryOnly)) {
                Require(++inspected<=10000&&watch.ElapsedMilliseconds<5000,"Zbyt wiele potwierdzeń operacji. Czyszczenie jest zablokowane.");
                if(path.EndsWith(".committed.json",StringComparison.Ordinal))continue;
                CheckArchiveFilePath(path);CheckArchiveFilePath(path+".committed.json");
                long journalSize=new FileInfo(path).Length;Require(journalSize<=65536&&(readBytes+=journalSize)<=64L*1024*1024,"Nieprawidłowe potwierdzenie operacji. Czyszczenie jest zablokowane.");
                string raw=File.ReadAllText(path,new UTF8Encoding(false,true));
                if(File.Exists(path+".committed.json")) {
                    long committedSize=new FileInfo(path+".committed.json").Length;Require(committedSize<=65536&&(readBytes+=committedSize)<=64L*1024*1024&&File.ReadAllText(path+".committed.json",new UTF8Encoding(false,true))==raw,"Niezgodne potwierdzenie operacji. Czyszczenie jest zablokowane.");continue;
                }
                JObject value;try{value=JObject.Parse(raw);}catch{throw new InvalidOperationException("Nieprawidłowe potwierdzenie operacji. Czyszczenie jest zablokowane.");}
                if((string)value["Operation"]=="savearchive"&&IsRankingArchiveId((string)value["ArchiveId"])) {
                    string savedPath=SafeArchivePath(directory,(string)value["ArchiveId"]);
                    if(File.Exists(savedPath))Require((readBytes+=new FileInfo(savedPath).Length)<=64L*1024*1024,"Zbyt wiele danych potwierdzeń operacji. Czyszczenie jest zablokowane.");
                    string savedHash;try{ReadManagedArchive(directory,(string)value["ArchiveId"],out savedHash);if(savedHash==(string)value["ArchiveHash"])continue;}catch { }
                }
                foreach(string id in new[]{(string)value["ArchiveId"],(string)value["BackupArchiveId"],value["Archive"]==null?null:(string)value["Archive"]["archiveId"]})if(!String.IsNullOrEmpty(id)){Require(IsRankingArchiveId(id),"Nieprawidłowe ID w potwierdzeniu operacji.");references.Add(id);}
            }
        }
        return references;
    }
    private ArchiveCleanupPlan BuildArchiveCleanupPlan(string directory,ArchiveCleanupRequest request,string preserveId) {
        Require(request!=null&&new[]{"manual","retention"}.Contains(request.mode)&&request.archiveIds!=null&&request.archiveIds.Count<=1000,"Nieprawidłowy wybór archiwów do usunięcia.");
        bool truncated;int skipped;var catalog=ScanArchiveMetadata(directory,out truncated,out skipped);
        Require(!truncated&&skipped==0,"Lista archiwów jest niepełna lub zawiera uszkodzone pliki. Czyszczenie jest zablokowane.");
        var protectedIds=PendingArchiveReferences(directory);if(!String.IsNullOrEmpty(preserveId))protectedIds.Add(preserveId);
        if(catalog.Count>0)protectedIds.Add(catalog.OrderByDescending(x=>x.createdAt).ThenBy(x=>x.archiveId,StringComparer.Ordinal).First().archiveId);
        var selected=new HashSet<string>(StringComparer.Ordinal);
        if(request.mode=="manual") {
            foreach(string id in request.archiveIds){Require(IsRankingArchiveId(id)&&selected.Add(id),"Nieprawidłowe lub powtórzone ID archiwum.");Require(catalog.Any(x=>x.archiveId==id),"Wybrane archiwum nie istnieje.");}
            Require(!selected.Any(protectedIds.Contains),"Wybrano najnowszą kopię rankingu lub archiwum używane przez niezakończoną operację. Zachowaj tę kopię.");
        } else {
            var policy=quizData.Settings.ArchiveRetention;long cutoff=Now()-policy.MaxAgeDays*86400000L;
            var eligible=new List<ArchiveMetadata>();
            foreach(var group in catalog.GroupBy(x=>x.type)) {
                if(group.Key=="manual"&&!policy.IncludeManual||group.Key=="safety"&&!policy.IncludeSafety)continue;
                var ordered=group.OrderByDescending(x=>x.createdAt).ThenBy(x=>x.archiveId,StringComparer.Ordinal).ToList();
                if(ordered.Count>0)protectedIds.Add(ordered[0].archiveId);
                eligible.AddRange(ordered);
            }
            foreach(var item in eligible.OrderByDescending(x=>x.createdAt).ThenBy(x=>x.archiveId,StringComparer.Ordinal).Select((value,index)=>new{value,index}))if((item.index>=policy.MaxCount||item.value.createdAt<cutoff)&&!protectedIds.Contains(item.value.archiveId))selected.Add(item.value.archiveId);
        }
        var plan=new ArchiveCleanupPlan{planId=Id(),expiresAt=Now()+300000,protectedCount=catalog.Count(x=>protectedIds.Contains(x.archiveId)),qualifiedCount=selected.Count,limited=selected.Count>MaxArchiveCleanupItems};long readBytes=0;
        foreach(var metadata in catalog.Where(x=>selected.Contains(x.archiveId)).OrderBy(x=>x.createdAt).ThenBy(x=>x.archiveId,StringComparer.Ordinal).Take(MaxArchiveCleanupItems)) {
            Require((readBytes+=metadata.sizeBytes)<=64L*1024*1024,"Wybrano zbyt wiele danych do jednego czyszczenia.");
            string hash;var archive=ReadManagedArchive(directory,metadata.archiveId,out hash);
            plan.items.Add(new ArchiveCleanupItem{archiveId=archive.ArchiveId,archiveHash=hash,name=metadata.name,type=metadata.type,sizeBytes=metadata.sizeBytes,createdAt=metadata.createdAt});
        }
        plan.planHash=ArchiveHash(JsonConvert.SerializeObject(plan));return plan;
    }
    private void PreviewArchiveCleanup(string directory) {
        var request=Read<ArchiveCleanupRequest>(GetStringArg("payloadJson"));var plan=BuildArchiveCleanupPlan(directory,request,"");
        foreach(string key in archiveCleanupPlans.Where(x=>x.Value.expiresAt<Now()).Select(x=>x.Key).ToList())archiveCleanupPlans.Remove(key);
        Require(archiveCleanupPlans.Count<20,"Zbyt wiele aktywnych podglądów czyszczenia. Poczekaj na ich wygaśnięcie.");archiveCleanupPlans[plan.planId]=plan;
        Broadcast(new{type="quiz-archive-cleanup-preview",build=BuildLabel,appVersion=AppVersion,operation="previewarchivecleanup",revision=quizData.Revision,requestId=requestId,planId=plan.planId,planHash=plan.planHash,expiresAt=plan.expiresAt,items=plan.items,totalBytes=plan.items.Sum(x=>x.sizeBytes),protectedCount=plan.protectedCount,qualifiedCount=plan.qualifiedCount,limited=plan.limited});
    }
    private string ArchiveDeletedMarker(string directory,string id) {
        string journal=Path.Combine(directory,".cleanup");CheckArchiveDirectoryPath(journal);
        string path=Path.Combine(journal,"deleted-"+ArchiveHash(id)+".json");CheckArchiveFilePath(path);return path;
    }
    private ArchiveCleanupResult ApplyArchiveCleanupPlan(string directory,ArchiveCleanupPlan plan,bool automatic) {
        Require(plan.expiresAt>=Now(),"Podgląd czyszczenia wygasł. Przygotuj go ponownie.");
        var selected=plan.items.Select(x=>x.archiveId).ToList();
        var fresh=BuildArchiveCleanupPlan(directory,new ArchiveCleanupRequest{mode="manual",archiveIds=selected},"");
        Require(fresh.items.Count==plan.items.Count&&fresh.items.All(x=>plan.items.Any(p=>p.archiveId==x.archiveId&&p.archiveHash==x.archiveHash&&p.sizeBytes==x.sizeBytes)),"Archiwum zmieniło się od podglądu. Przygotuj go ponownie.");
        bool truncated;int skipped;var catalog=ScanArchiveMetadata(directory,out truncated,out skipped);
        var survivor=catalog.Where(x=>!selected.Contains(x.archiveId)).OrderByDescending(x=>x.createdAt).FirstOrDefault();
        Require(plan.items.Count==0||survivor!=null,"Nie można usunąć wszystkich kopii rankingu.");
        string survivorHash;if(survivor!=null)ReadManagedArchive(directory,survivor.archiveId,out survivorHash);
        var result=new ArchiveCleanupResult{planId=plan.planId,automatic=automatic};
        string staging=Path.Combine(directory,".cleanup");CheckArchiveDirectoryPath(staging);Directory.CreateDirectory(staging);CheckArchiveDirectoryPath(staging);
        foreach(var item in plan.items) {
            string path=SafeArchivePath(directory,item.archiveId),temporary=Path.Combine(staging,"pending-"+plan.planId+"-"+ArchiveHash(item.archiveId)+".json");
            try {
                CheckArchiveFilePath(temporary);Require(!File.Exists(temporary),"Plik roboczy czyszczenia już istnieje.");
                string currentHash;ReadManagedArchive(directory,item.archiveId,out currentHash);Require(currentHash==item.archiveHash,"Archiwum zmieniło się podczas czyszczenia.");
                WriteArchiveFile(ArchiveDeletedMarker(directory,item.archiveId),JsonConvert.SerializeObject(new{schema=1,archiveId=item.archiveId,archiveHash=item.archiveHash,deletedAt=Now()}));
                CheckArchiveFilePath(path);CheckArchiveFilePath(temporary);File.Move(path,temporary);
                CheckArchiveFilePath(temporary);Require(ArchiveHash(File.ReadAllText(temporary,new UTF8Encoding(false,true)))==item.archiveHash,"Plik zmienił się podczas przenoszenia do usunięcia.");
                File.Delete(temporary);result.deletedCount++;result.deletedBytes+=item.sizeBytes;
            }catch(Exception) {
                result.failedCount++;
                try{CheckArchiveFilePath(temporary);CheckArchiveFilePath(path);if(File.Exists(temporary)&&!File.Exists(path))File.Move(temporary,path);string restoredHash;if(File.Exists(path)){ReadManagedArchive(directory,item.archiveId,out restoredHash);if(restoredHash==item.archiveHash){string marker=ArchiveDeletedMarker(directory,item.archiveId);CheckArchiveFilePath(marker);if(File.Exists(marker))File.Delete(marker);}}}catch{CPH.LogError("[LukiQuiz V2] Cleanup recovery required for "+item.archiveId);}
            }
        }
        return result;
    }
    private void DeleteArchiveCleanupPlan(string directory) {
        Confirm("USUN ARCHIWA");ArchiveCleanupPlan plan;string id=GetStringArg("planId");
        Require(archiveCleanupPlans.TryGetValue(id,out plan)&&plan.planHash==GetStringArg("planHash"),"Brak zgodnego podglądu czyszczenia. Przygotuj go ponownie.");
        archiveCleanup=ApplyArchiveCleanupPlan(directory,plan,false);archiveCleanupPlans.Remove(id);
    }
    private void TryAutomaticArchiveCleanup(string directory,string preserveId) {
        if(!quizData.Settings.ArchiveRetention.Enabled||quizData.Current!=null&&(quizData.Current.Status=="open"||quizData.Current.Status=="locked"))return;
        try{var plan=BuildArchiveCleanupPlan(directory,new ArchiveCleanupRequest{mode="retention"},preserveId);if(plan.items.Count>0){var result=ApplyArchiveCleanupPlan(directory,plan,true);CPH.LogInfo("[LukiQuiz V2] Automatic archive cleanup deleted="+result.deletedCount+" failed="+result.failedCount);}}
        catch(Exception ex){CPH.LogWarn("[LukiQuiz V2] Automatic archive cleanup skipped: "+SafeError(ex));}
    }
    private ArchiveMetadata ReadArchiveMetadata(string path) {
        var scalars=new JObject();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allowed=new HashSet<string>(typeof(RankingArchiveDocument).GetProperties().Select(x=>x.Name),StringComparer.OrdinalIgnoreCase);
        using(var stream=new StreamReader(path,new UTF8Encoding(false,true)))using(var reader=new JsonTextReader(stream)) {
            reader.MaxDepth=64;reader.DateParseHandling=DateParseHandling.None;Require(reader.Read()&&reader.TokenType==JsonToken.StartObject,"Nieprawidłowe archiwum.");
            bool ended=false;while(reader.Read()) {
                if(reader.TokenType==JsonToken.EndObject){ended=true;break;}
                Require(reader.TokenType==JsonToken.PropertyName,"Nieprawidłowe pole archiwum.");string name=(string)reader.Value;Require(names.Add(name)&&allowed.Contains(name),"Zduplikowane lub nieznane pole archiwum.");Require(reader.Read(),"Niepełne archiwum.");
                if(new[]{"Scores","Members","TeamScores","BaseTeamScores"}.Contains(name))Require(reader.TokenType==JsonToken.StartObject,"Nieprawidłowy format rankingu archiwum.");
                if(name=="Teams")Require(reader.TokenType==JsonToken.StartArray,"Nieprawidłowy format drużyn archiwum.");
                if(reader.TokenType==JsonToken.StartObject||reader.TokenType==JsonToken.StartArray)reader.Skip();else {Require(reader.TokenType!=JsonToken.Comment,"Komentarz archiwum.");scalars[name]=reader.Value==null?JValue.CreateNull():new JValue(reader.Value);}
            }Require(ended&&!reader.Read(),"Dodatkowe lub niepełne dane archiwum.");
        }
        Require((string)scalars["Format"]=="LukiQuizRankingArchive"&&scalars["Schema"]!=null&&scalars["Schema"].Type==JTokenType.Integer&&(int)scalars["Schema"]==1,"Nieobsługiwane archiwum.");
        foreach(string key in new[]{"ArchiveId","CreatedAtUtc","AppVersion","Build","EnginePatch","SourceStateHash"})Require(scalars[key]!=null&&scalars[key].Type==JTokenType.String,"Niepełne metadane archiwum.");
        foreach(string key in new[]{"CreatedAt","Users","Epoch","Revision"})Require(scalars[key]!=null&&scalars[key].Type==JTokenType.Integer,"Niepełne metadane archiwum.");
        Require(names.Contains("Scores")&&names.Contains("Teams")&&names.Contains("Members")&&names.Contains("TeamScores")&&names.Contains("BaseTeamScores"),"Niepełne archiwum.");
        var archive=scalars.ToObject<RankingArchiveDocument>();Require(archive.ArchiveId==Path.GetFileNameWithoutExtension(path)&&IsRankingArchiveId(archive.ArchiveId),"Niepoprawne ID archiwum.");
        ValidateRange(archive.Users,0,25000,"Liczba wyników");ValidateRange(archive.CreatedAt,1,4102444800000L,"Data archiwum");ValidateRange(archive.Epoch,0,Int32.MaxValue-1,"Epoch archiwum");ValidateRange(archive.Revision,0,Int32.MaxValue-1,"Revision archiwum");
        DateTime created;Require(DateTime.TryParse(archive.CreatedAtUtc,System.Globalization.CultureInfo.InvariantCulture,System.Globalization.DateTimeStyles.RoundtripKind,out created)&&created.Kind==DateTimeKind.Utc&&Math.Abs((created-new DateTime(1970,1,1,0,0,0,DateTimeKind.Utc)).TotalMilliseconds-archive.CreatedAt)<2000,"Nieprawidłowa data archiwum.");
        Require(new[]{"2.0.0","2.0.1","2.0.2","2.1.0","2.2.0"}.Contains(archive.AppVersion)&&new[]{"FC1","STYLE4","PHoeniX","QUPA"}.Contains(archive.Build)&&new[]{"FC1","STYLE4","PHoeniX","QUPA"}.Contains(archive.EnginePatch),"Nieobsługiwane oznaczenie wersji archiwum.");
        Require(Regex.IsMatch(archive.SourceStateHash??"",@"^[a-f0-9]{64}$"),"Nieprawidłowy SourceStateHash archiwum.");
        string type=ArchiveType(archive);Require(new[]{"automatic","manual","safety"}.Contains(type)&&archive.ArchiveId.StartsWith(type=="automatic"?"ranking-":type+"-",StringComparison.Ordinal),"Typ archiwum.");
        if(type=="automatic")Require(archive.ArchiveId.EndsWith("-"+archive.SourceStateHash,StringComparison.Ordinal),"Niezgodny SourceStateHash archiwum.");
        if(archive.Name!=null){ValidateText(archive.Name,1,80,"Nazwa archiwum");Require(!archive.Name.Any(Char.IsControl),"Nieprawidłowa nazwa archiwum.");}
        if(archive.ContentHash!=null)Require(Regex.IsMatch(archive.ContentHash,@"^[a-f0-9]{64}$"),"Nieprawidłowy hash archiwum.");
        if(archive.TotalPoints.HasValue)ValidateRange(archive.TotalPoints.Value,0,2500000000000L,"Suma punktów archiwum");
        return ArchiveInfo(archive);
    }
    public class ResetArchiveIntent {
        public int Schema {get;set;}=1;
        public string RequestId {get;set;}
        public string SourceHash {get;set;}
        public string TargetHash {get;set;}
        public int TargetRevision {get;set;}
        public int TargetEpoch {get;set;}
        public ArchiveResetResult Archive {get;set;}
        public string ArchiveHash {get;set;}
    }
    private void EnsureRoundFinished() { Require(quizData.Current==null || quizData.Current.Status=="revealed" || quizData.Current.Status=="cancelled","Zakończ/anuluj aktywną rundę przed tą operacją."); }
    private void Confirm(string text) { Require(String.Equals((GetStringArg("confirmText")??"").Trim(),text,StringComparison.OrdinalIgnoreCase),"Wymagane potwierdzenie: "+text); }
    private void NewEpoch(Dictionary<string,ScoreEntry> scores) {
        quizData.Epoch++; quizData.BaseScores=Copy(scores); quizData.Scores=Copy(scores); quizData.BaseTeamScores=Copy(quizData.TeamScores);
        if(quizData.Current!=null&&(quizData.Current.Status=="open"||quizData.Current.Status=="locked")) quizData.Current.Epoch=quizData.Epoch;
    }

    private string CurrentExpirationError() { return String.Equals(storedStateJson,failedExpirationStateJson,StringComparison.Ordinal) ? expirationError : ""; }
    private bool TryExpireAndSave() {
        if(CurrentExpirationError()!="") return false;
        var r=quizData.Current;
        if(r==null || r.Status!="open" || r.Paused || r.ClosesAt<=0 || Now()<r.ClosesAt) return false;
        var previousData=quizData; string previousRaw=storedStateJson;
        try {
            quizData=Copy(previousData);
            if(!Expire()) {quizData=previousData;return false;}
            SaveState();
            if(quizData.Current!=null&&quizData.Current.Status=="revealed")AnnounceRevealedRound(quizData.Current);
            failedExpirationStateJson=null;expirationError="";
            return true;
        } catch(Exception ex) {
            quizData=null;quizData=LoadState();
            failedExpirationStateJson=previousRaw;
            expirationError="Automatyczne zakończenie rundy zgłosiło błąd: "+SafeError(ex)+" Automatyczne ponawianie dla tego stanu jest wstrzymane. Odczyt danych i diagnostyka pozostają dostępne. Przed przywróceniem kopii anuluj nierozliczoną rundę.";
            CPH.LogError("[LukiQuiz V2] "+expirationError);
            BroadcastNotice(expirationError);
            return false;
        }
    }
    private bool Expire() {
        var r=quizData.Current;
        if(r==null || r.Status!="open" || r.Paused || r.ClosesAt<=0 || Now()<r.ClosesAt) return false;
        r.ElapsedMs=Elapsed(r); r.ClockAt=Now(); r.RemainingMs=0; r.ClosesAt=0; r.Paused=false; r.Status="locked";
        if(r.Question.AutoReveal) Reveal(r,null);
        quizData.Revision++; return true;
    }
    private long Elapsed(Round r) { return r.ElapsedMs+(r.Status=="open"&&!r.Paused?Math.Max(0,Now()-r.ClockAt):0); }
    private void ResolvePartial(Question q) {
        if(q.Scoring=="adjacent") for(int i=0;i<q.Options.Count;i++) q.Options[i].Partial=!q.Options[i].Correct&&q.Options.Where((o,j)=>Math.Abs((q.Options[i].AuthorIndex>0?q.Options[i].AuthorIndex:i+1)-(o.AuthorIndex>0?o.AuthorIndex:j+1))==1).Any(o=>o.Correct);
        if(q.Scoring=="none") foreach(var o in q.Options) o.Partial=false;
    }
    private void Start(Question q) {
        EnsureRoundFinished(); ValidateQuestion(q); q=Copy(q); for(int i=0;i<q.Options.Count;i++)q.Options[i].AuthorIndex=i+1; ResolvePartial(q);
        if(q.Shuffle) { var random=new Random(); for(int i=q.Options.Count-1;i>0;i--) { int j=random.Next(i+1); var tmp=q.Options[i]; q.Options[i]=q.Options[j]; q.Options[j]=tmp; } }
        quizData.Podium=false;quizData.OverlayVisible=true; quizData.Current=new Round {Id=Id(),Question=q,StartedAt=Now(),ClockAt=Now(),ClosesAt=q.Duration>0?Now()+q.Duration*1000L:0,Epoch=quizData.Epoch,Competitive=new CompetitiveRound {Settings=Copy(quizData.Competitive.Settings)}};
        quizData.PreparedQueueIndex=-1;
        quizData.Revision++; SaveState(); BroadcastState(); if(quizData.Settings.AnnounceRoundEventsInChat)SendChat("Quiz: "+q.Text+" - głosuj !1-!"+q.Options.Count);
    }
    private void StartQuiz(string question,string answersJson,int durationSeconds,bool announce) {
        var texts=Read<List<string>>(answersJson);
        Start(new Question {Text=question,Duration=durationSeconds,FullPoints=1,PartialPoints=0,Options=texts.Select(t=>new Option {Text=t}).ToList()});
    }
    private void LockQuiz(bool expired,bool announce) {
        var r=quizData.Current; Require(r!=null,"Brak rundy.");
        if(expired) { if(TryExpireAndSave()) BroadcastState(); Require(CurrentExpirationError()=="",CurrentExpirationError()); return; }
        Require(r.Status=="open"||r.Status=="locked","Brak otwartej rundy.");
        if(r.Status=="open") {
            long now=Now();
            r.ElapsedMs=Elapsed(r);
            if(r.Question.Duration>0) {
                r.RemainingMs=r.Paused?r.RemainingMs:(r.ClosesAt>0?Math.Max(0,r.ClosesAt-now):0);
            } else r.RemainingMs=0;
            r.ClockAt=now; r.ClosesAt=0; r.Paused=false; r.Status="locked";
        }
        quizData.Revision++; SaveState(); BroadcastState();if(announce&&quizData.Settings.AnnounceRoundEventsInChat)SendChat("Quiz: głosowanie zamknięte.");
    }
    private void ReopenQuiz(bool announce) {
        var r=quizData.Current; Require(r!=null&&r.Status=="locked","Ponownie otworzyć można tylko zamknięte głosowanie przed reveal.");
        long now=Now();
        r.Status="open"; r.ClockAt=now; r.Paused=false;
        if(r.Question.Duration>0 && r.RemainingMs>0) {
            r.ClosesAt=now+r.RemainingMs; r.RemainingMs=0;
        } else {
            // Jeżeli timer zdążył dojść do zera, ponowne otwarcie przechodzi w tryb ręczny,aby runda nie zamknęła się natychmiast ponownie.
            r.ClosesAt=0; r.RemainingMs=0;
        }
        quizData.Revision++; SaveState(); BroadcastState();
        if(announce&&quizData.Settings.AnnounceRoundEventsInChat)SendChat("Quiz: głosowanie ponownie otwarte.");
    }
    private List<AchievementDefinition> AchievementDefinitions() {
        return new List<AchievementDefinition> {
            new AchievementDefinition{Id="first-correct",Metric="correct",Threshold=1},
            new AchievementDefinition{Id="correct-10",Metric="correct",Threshold=10},
            new AchievementDefinition{Id="correct-25",Metric="correct",Threshold=25},
            new AchievementDefinition{Id="correct-50",Metric="correct",Threshold=50},
            new AchievementDefinition{Id="streak-3",Metric="streak",Threshold=3,RequiresStreak=true},
            new AchievementDefinition{Id="streak-5",Metric="streak",Threshold=5,RequiresStreak=true},
            new AchievementDefinition{Id="streak-10",Metric="streak",Threshold=10,RequiresStreak=true},
            new AchievementDefinition{Id="participation-10",Metric="participation",Threshold=10},
            new AchievementDefinition{Id="participation-50",Metric="participation",Threshold=50}
        };
    }
    private void ValidateCompetitiveSettings(CompetitiveSettings settings) {
        Require(settings!=null&&settings.StreakBonuses!=null,"Missing competitive settings.");
        Require(settings.StreakBonuses.Count<=20,"At most 20 streak thresholds are allowed.");
        int previous=0;foreach(var bonus in settings.StreakBonuses) {
            Require(bonus!=null,"Missing streak threshold.");ValidateRange(bonus.Threshold,1,10000,"Streak threshold");
            ValidateRange(bonus.Points,0,1000,"Streak bonus");Require(bonus.Threshold>previous,"Streak thresholds must be unique and ascending.");previous=bonus.Threshold;
        }
    }
    private void ValidateCompetitiveProjection(CompetitiveProjection projection) {
        Require(projection!=null&&projection.Users!=null&&projection.SeasonScores!=null,"Missing competitive projection.");
        Require(projection.Users.Count<=25000&&projection.SeasonScores.Count<=1,"Competitive data limit reached.");
        var definitions=AchievementDefinitions().Select(x=>x.Id).ToList();
        foreach(var item in projection.Users) {
            UserId(item.Key);var user=item.Value;Require(user!=null&&user.UserId==item.Key&&user.Achievements!=null,"Invalid competitive viewer.");
            ValidateText(user.UserName,1,80,"Competitive viewer");ValidateRange(user.CurrentStreak,0,100000000,"Current streak");
            ValidateRange(user.BestStreak,user.CurrentStreak,100000000,"Best streak");ValidateRange(user.StreakCorrectAnswers,user.BestStreak,100000000,"Streak correct answers");
            ValidateRange(user.AchievementRounds,0,100000000,"Achievement participation");ValidateRange(user.AchievementCorrectAnswers,0,user.AchievementRounds,"Achievement correct answers");
            ValidateRange(user.AchievementStreak,0,user.AchievementCorrectAnswers,"Achievement streak");Require(user.Achievements.Count<=definitions.Count,"Achievement limit reached.");
            foreach(var achievement in user.Achievements){Require(definitions.Contains(achievement.Key),"Unknown achievement.");ValidateRange(achievement.Value,1,4102444800000L,"Achievement timestamp");}
        }
        foreach(var season in projection.SeasonScores){Identifier(season.Key);Require(season.Value!=null&&season.Value.Count<=25000,"Season score limit reached.");foreach(var user in season.Value)ValidateScore(user.Value,user.Key);}
    }
    private void ValidateCompetitive(CompetitiveState competitive) {
        Require(competitive!=null&&competitive.Schema==1&&competitive.Seasons!=null&&competitive.FrozenRounds!=null,"Invalid competitive state.");
        Identifier(competitive.DataId);Identifier(competitive.ActiveSeasonId,true);ValidateCompetitiveSettings(competitive.Settings);
        ValidateCompetitiveProjection(competitive.Base);ValidateCompetitiveProjection(competitive.Stats);
        Require(competitive.Seasons.Count<=100&&competitive.FrozenRounds.Count<=MaxHistory,"Season or frozen history limit reached.");
        var ids=new HashSet<string>();int active=0;
        foreach(var season in competitive.Seasons) {
            Require(season!=null&&season.Scores!=null&&season.Winners!=null,"Missing season.");Identifier(season.Id);Require(ids.Add(season.Id),"Duplicate season ID.");
            ValidateText(season.Name,1,80,"Season name");Require(season.Kind=="season"||season.Kind=="event","Invalid season kind.");
            Require(new[]{"created","active","closed"}.Contains(season.Status),"Invalid season status.");Identifier(season.CreateRequestId,true);
            ValidateRange(season.CreatedAt,1,4102444800000L,"Season created");ValidateRange(season.StartedAt,0,4102444800000L,"Season start");ValidateRange(season.EndedAt,0,4102444800000L,"Season end");
            if(season.Status=="created")Require(season.StartedAt==0&&season.EndedAt==0&&season.Scores.Count==0&&season.Winners.Count==0,"A created season cannot contain results.");
            if(season.Status=="active"){active++;Require(season.Id==competitive.ActiveSeasonId&&season.StartedAt>0&&season.EndedAt==0&&season.Scores.Count==0&&season.Winners.Count==0,"Invalid active season.");}
            if(season.Status=="closed")Require(season.StartedAt>0&&season.EndedAt>=season.StartedAt,"Invalid closed season timestamps.");
            Require(season.Scores.Count<=25000&&season.Winners.Count<=25000,"Season score limit reached.");foreach(var score in season.Scores)ValidateScore(score.Value,score.Key);
            var winnerIds=new HashSet<string>();int best=season.Scores.Count==0?0:season.Scores.Values.Max(x=>x.Points);
            foreach(var winner in season.Winners){Require(winner!=null&&season.Scores.ContainsKey(winner.UserId)&&winnerIds.Add(winner.UserId),"Invalid season winner.");ValidateText(winner.UserName,1,80,"Winner name");ValidateRange(winner.Points,0,100000000,"Winner points");Require(season.Scores[winner.UserId].Points==winner.Points&&winner.Points==best&&season.Scores[winner.UserId].UserName==winner.UserName,"Winner score mismatch.");}
            if(season.Status=="closed")Require(winnerIds.SetEquals(season.Scores.Values.Where(x=>x.Points==best).Select(x=>x.UserId)),"Season winners are incomplete.");
        }
        Require(active==(competitive.ActiveSeasonId==""?0:1),"Active season mismatch.");
        foreach(var key in competitive.Base.SeasonScores.Keys.Concat(competitive.Stats.SeasonScores.Keys))Require(key==competitive.ActiveSeasonId,"A projection references an inactive season.");
        var frozen=new HashSet<string>();foreach(string id in competitive.FrozenRounds){Identifier(id);Require(frozen.Add(id),"Duplicate frozen round ID.");}
    }
    private CompetitiveUser EnsureCompetitiveUser(CompetitiveProjection projection,string id,string name) {
        CompetitiveUser user;if(projection.Users.TryGetValue(id,out user)){user.UserName=name;return user;}
        Require(projection.Users.Count<25000,"Competitive viewer limit reached.");
        user=new CompetitiveUser{UserId=id,UserName=name};projection.Users[id]=user;return user;
    }
    private Dictionary<string,int> ApplyCompetitiveRound(Round round,CompetitiveProjection projection) {
        var bonuses=new Dictionary<string,int>();var snapshot=round.Competitive;
        if(snapshot==null||!round.Applied||round.Question.Mode!="QUIZ")return bonuses;
        var settings=snapshot.Settings;
        if(settings.StreakEnabled)foreach(var user in projection.Users.Values.Where(x=>!round.Votes.ContainsKey(x.UserId))){user.CurrentStreak=0;user.AchievementStreak=0;}
        foreach(var vote in round.Votes.OrderBy(x=>x.Key,StringComparer.Ordinal)) {
            var option=round.Question.Options.First(x=>x.Id==vote.Value.OptionId);int bonus=0;
            if(settings.StreakEnabled||settings.AchievementsEnabled) {
                var user=EnsureCompetitiveUser(projection,vote.Key,vote.Value.Name);
                if(settings.StreakEnabled) {
                    if(option.Correct){user.CurrentStreak=checked(user.CurrentStreak+1);user.StreakCorrectAnswers=checked(user.StreakCorrectAnswers+1);user.BestStreak=Math.Max(user.BestStreak,user.CurrentStreak);}
                    else {user.CurrentStreak=0;user.AchievementStreak=0;}
                    if(option.Correct){var threshold=settings.StreakBonuses.FirstOrDefault(x=>x.Threshold==user.CurrentStreak);if(threshold!=null)bonus=Math.Min(threshold.Points,Math.Max(0,2000-Points(round.Question,option,vote.Value)));}
                }
                if(settings.AchievementsEnabled) {
                    user.AchievementRounds=checked(user.AchievementRounds+1);if(option.Correct)user.AchievementCorrectAnswers=checked(user.AchievementCorrectAnswers+1);
                    if(settings.StreakEnabled)user.AchievementStreak=option.Correct?Math.Min(user.CurrentStreak,checked(user.AchievementStreak+1)):0;
                    foreach(var definition in AchievementDefinitions()) {
                        if(definition.RequiresStreak&&!settings.StreakEnabled)continue;
                        int metric=definition.Metric=="correct"?user.AchievementCorrectAnswers:definition.Metric=="participation"?user.AchievementRounds:user.AchievementStreak;
                        if(metric>=definition.Threshold&&!user.Achievements.ContainsKey(definition.Id))user.Achievements[definition.Id]=round.EndedAt;
                    }
                }
            }
            if(bonus>0)bonuses[vote.Key]=bonus;
        }
        return bonuses;
    }
    private void UpdateRoundSummary(Round round) {
        var summary=new RoundSummary{Participants=round.Votes.Count,AcceptedVotes=round.Votes.Count,Status=round.Status=="revealed"&&round.Question.Mode=="QUIZ"&&!round.Applied?"undone":round.Status};
        if(round.Status=="revealed"&&round.Question.Mode=="QUIZ") {
            summary.CorrectAnswers=round.Votes.Count(x=>round.Question.Options.Any(o=>o.Id==x.Value.OptionId&&o.Correct));
            summary.CorrectPercent=round.Votes.Count==0?(double?)null:Math.Round(summary.CorrectAnswers.Value*100.0/round.Votes.Count,2);
            summary.TotalPoints=round.Applied?round.Awards.Values.Sum(x=>(long)x.Points):0;
            summary.TopPoints=round.Applied&&round.Awards.Count>0?round.Awards.Values.Max(x=>x.Points):0;
        }
        round.Summary=summary;
    }
    private void PrepareNext() {
        EnsureRoundFinished();Require(quizData.Queue!=null&&quizData.Queue.Count>0,"Load a question set first.");
        Require(GetStringArg("expectedSetId")==quizData.QueueSetId,"The loaded set changed. Refresh the panel.");
        int expected=GetIntArg("expectedQueueIndex",Int32.MinValue);
        if(quizData.PreparedQueueIndex>=0&&quizData.PreparedQueueIndex==quizData.QueueIndex){Require(expected==quizData.QueueIndex||expected==quizData.QueueIndex-1,"The queue changed. Refresh the panel.");return;}
        Require(expected==quizData.QueueIndex,"The queue changed. Refresh the panel.");int next=quizData.QueueIndex+1;
        Require(next<quizData.Queue.Count,"The question set is finished.");
        quizData.QueueIndex=next;quizData.PreparedQueueIndex=next;quizData.Draft=Copy(quizData.Queue[next]);quizData.Podium=false;
    }
    private object CompetitiveSettingsPayload(CompetitiveSettings settings) {return new{StreakEnabled=settings.StreakEnabled,AchievementsEnabled=settings.AchievementsEnabled,StreakBonuses=settings.StreakBonuses};}
    private object CompetitiveProgress(CompetitiveState competitive) {return new{settings=CompetitiveSettingsPayload(competitive.Settings),baseUsers=competitive.Base.Users,users=competitive.Stats.Users};}
    private void BroadcastCompetitive() {Broadcast(new{type="quiz-competitive",settings=CompetitiveSettingsPayload(quizData.Competitive.Settings),catalog=AchievementDefinitions(),users=quizData.Competitive.Stats.Users});}
    private bool HasCompetitiveData(CompetitiveState competitive) {
        return competitive.Base.Users.Count>0||competitive.Stats.Users.Count>0||competitive.Settings.StreakEnabled||competitive.Settings.AchievementsEnabled;
    }
    private void CheckCompetitiveRestore(Root restored) {
        var current=quizData.Competitive;
        if(!HasCompetitiveData(current))return;
        Require(restored.Competitive.DataId==current.DataId,"This backup belongs to older or different competitive data. Restore was stopped to preserve streaks and achievements.");
        Require(JsonConvert.SerializeObject(CompetitiveProgress(restored.Competitive))==JsonConvert.SerializeObject(CompetitiveProgress(current)),"This backup conflicts with current competitive progress. Restore was stopped to preserve streaks and achievements.");
    }

    private void Reveal(Round r,Question config) {
        Require(r.Status=="open"||r.Status=="locked","Runda już rozliczona/anulowana.");
        if(config!=null) {
            ValidateQuestion(config); Require(config.Options.Select(o=>o.Id).SequenceEqual(r.Question.Options.Select(o=>o.Id)),"Nie zmieniaj mapowania aktywnych odpowiedzi.");
            r.Question=Copy(config); ResolvePartial(r.Question);
        }
        Require(r.Question.Mode=="POLL"||r.Question.Options.Any(o=>o.Correct),"Wskaż co najmniej jedną poprawną odpowiedź.");
        r.ElapsedMs=Elapsed(r); r.ClockAt=Now(); r.Status="revealed"; r.EndedAt=Now(); r.Applied=r.Question.Mode=="QUIZ"; r.Legacy=false;
        Require(!quizData.History.Any(x=>x.Id==r.Id),"Round ID już w historii.");
        quizData.History.Add(Copy(r)); Replay(); Compact(); quizData.Current=Copy(quizData.History.Last());Compact();
    }
    private void AnnounceRevealedRound(Round r) {
        if(r!=null&&r.Status=="revealed"&&quizData.Settings.AnnounceRoundEventsInChat)SendChat(r.Question.Mode=="POLL"?"Ankieta zakończona.":"Quiz: wynik ujawniony, punkty przyznane.");
    }
    private void RevealAnswer(int correctIndex,int full,int partial,string mode,string partialJson,bool announce) {
        Require(quizData.Current!=null,"Brak rundy."); var q=Copy(quizData.Current.Question); ValidateRange(correctIndex,1,q.Options.Count,"Poprawna");
        q.FullPoints=full; q.PartialPoints=partial; q.Scoring=mode=="manual"?"manual":"adjacent";
        var indices=Read<List<int>>(partialJson); foreach(var o in q.Options) {o.Correct=false;o.Partial=false;o.Points=null;}
        q.Options[correctIndex-1].Correct=true; foreach(int n in indices) {ValidateRange(n,1,q.Options.Count,"Partial index");q.Options[n-1].Partial=true;}
        Reveal(quizData.Current,q); quizData.Revision++; SaveState(); BroadcastState(); BroadcastScores(); AnnounceRevealedRound(quizData.Current);
    }
    private Dictionary<string,ScoreEntry> LoadScores() { return quizData.Scores; }
    private List<ScoreEntry> SortScores(Dictionary<string,ScoreEntry> scores) {return scores.Values.OrderByDescending(x=>x.Points).ThenByDescending(x=>x.CorrectAnswers).ThenBy(x=>x.AnsweredQuestions).ThenBy(x=>x.UserName,StringComparer.OrdinalIgnoreCase).ThenBy(x=>x.UserId,StringComparer.Ordinal).ToList();}
    private ScoreEntry EnsureScore(Dictionary<string,ScoreEntry> scores,string id,string name) {
        ScoreEntry entry; if(scores.TryGetValue(id,out entry)) return entry;
        var candidates=scores.Where(x=>x.Key.StartsWith("name:")&&NormalizeScoreName(x.Value.UserName)==NormalizeScoreName(name)).ToList();
        Require(candidates.Count<=1,"Niejednoznaczne dopasowanie starej nazwy.");
        if(candidates.Count==1 && !id.StartsWith("name:")) { entry=candidates[0].Value;scores.Remove(candidates[0].Key);entry.UserId=id;scores[id]=entry;return entry; }
        entry=new ScoreEntry {UserId=id,UserName=name}; scores[id]=entry; return entry;
    }
    private int Points(Question q,Option o,Vote v) {
        int p=o.Points.HasValue?o.Points.Value:o.Correct?q.FullPoints:o.Partial?q.PartialPoints:0;
        if(q.Speed&&o.Correct) p+=v.ElapsedMs<=5000?q.SpeedFast:v.ElapsedMs<=10000?q.SpeedSlow:0;
        return p;
    }
    private void ApplyRound(Round r,Dictionary<string,ScoreEntry> scores,Dictionary<string,long> teams,Dictionary<string,int> competitiveBonuses=null) {
        r.Awards.Clear(); if(!r.Applied || r.Question.Mode!="QUIZ") return;
        foreach(var v in r.Votes.OrderBy(x=>x.Key,StringComparer.Ordinal)) {
            ScoreEntry e=EnsureScore(scores,v.Key,v.Value.Name); var before=Copy(e); var o=r.Question.Options.First(x=>x.Id==v.Value.OptionId);
            int bonus;int points=Points(r.Question,o,v.Value)+(competitiveBonuses!=null&&competitiveBonuses.TryGetValue(v.Key,out bonus)?bonus:0); e.UserName=v.Value.Name; e.Points=checked(e.Points+points); e.AnsweredQuestions++;
            if(o.Correct)e.CorrectAnswers++;
            e.LastUpdatedAt=r.EndedAt; r.Awards[v.Key]=new Award {Points=points,Correct=o.Correct,Before=before,After=Copy(e)};
            if(!String.IsNullOrEmpty(v.Value.TeamId)) {if(!teams.ContainsKey(v.Value.TeamId))teams[v.Value.TeamId]=0;teams[v.Value.TeamId]+=points;}
        }
    }
    private void Replay() {
        var scores=Copy(quizData.BaseScores);var teams=Copy(quizData.BaseTeamScores);
        var competitive=Copy(quizData.Competitive.Base);
        competitive.SeasonScores=Copy(quizData.Competitive.Stats.SeasonScores);
        foreach(var r in quizData.History) {
            var bonuses=ApplyCompetitiveRound(r,competitive);
            if(r.Epoch==quizData.Epoch)ApplyRound(r,scores,teams,bonuses);
            UpdateRoundSummary(r);
        }
        quizData.Scores=scores;quizData.TeamScores=teams;quizData.Competitive.Stats=competitive;
    }
    private void Compact() {
        while(quizData.History.Count>MaxHistory || (quizData.History.Count>1 && Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(quizData))>6*1024*1024)) {
            var r=quizData.History[0];var bonuses=ApplyCompetitiveRound(r,quizData.Competitive.Base);if(r.Epoch==quizData.Epoch) ApplyRound(r,quizData.BaseScores,quizData.BaseTeamScores,bonuses);quizData.History.RemoveAt(0);
        }
    }
    private void Undo() {
        EnsureRoundFinished(); var r=quizData.History.LastOrDefault(x=>x.Applied && x.Epoch==quizData.Epoch && !x.Legacy);
        Require(r!=null,"Brak wyniku możliwego do cofnięcia w bieżącej epoce.");
        Require(GetStringArg("roundId")==r.Id,"Wynik zmienił się - odśwież historię.");

        r.Applied=false; Replay(); if(quizData.Current!=null&&quizData.Current.Id==r.Id)quizData.Current=Copy(r);
    }
    private void Rescore() {
        EnsureRoundFinished();var r=quizData.History.FirstOrDefault(x=>x.Id==GetStringArg("roundId"));
        Require(r!=null && r.Epoch==quizData.Epoch && !r.Legacy && r.Question.Mode=="QUIZ","Ten wynik jest archiwalny/POLL i nie może być przeliczony.");

        var q=Read<Question>(GetStringArg("payloadJson"));ValidateQuestion(q);
        Require(q.Mode=="QUIZ"&&q.Options.Select(o=>o.Id).SequenceEqual(r.Question.Options.Select(o=>o.Id))&&q.Text==r.Question.Text&&q.Options.Select(o=>o.Text).SequenceEqual(r.Question.Options.Select(o=>o.Text)),"Rescore zmienia punktację, nie treść/mapowanie.");
        Require(q.Options.Any(o=>o.Correct),"Brak poprawnej odpowiedzi."); ResolvePartial(q);r.Question=q;r.Applied=true;Replay();if(quizData.Current!=null&&quizData.Current.Id==r.Id)quizData.Current=Copy(r);
    }
    private void CancelQuiz(bool announce) { if(quizData.Current!=null&&(quizData.Current.Status=="open"||quizData.Current.Status=="locked")) {quizData.Current.Status="cancelled";quizData.Current.EndedAt=Now();UpdateRoundSummary(quizData.Current);quizData.History.Add(Copy(quizData.Current));Compact();} quizData.Current=null;quizData.Podium=false;quizData.Revision++;SaveState(CurrentExpirationError()!="");BroadcastState();BroadcastData(); }
    private void SetOverlayVisibility(bool show) {Require(quizData.Current!=null||quizData.Podium,"Brak rundy/podium.");quizData.OverlayVisible=show;if(quizData.Current!=null)quizData.Current.Visible=show;quizData.Revision++;SaveState();BroadcastState();}
    private void RegisterVote(int index) {
        var r=quizData.Current; if(r==null||r.Status!="open"||index<1||index>r.Question.Options.Count)return;
        Require(CurrentExpirationError()=="",CurrentExpirationError());
        string id=GetStringArg("userId");string name=GetDisplayName();if(String.IsNullOrWhiteSpace(id)){string login=GetStringArg("userName").Trim();if(login=="")return;id="name:"+login.ToLowerInvariant();}
        UserId(id);ValidateText(name,1,80,"Nazwa widza");Vote v;
        if (!id.StartsWith("name:") && !quizData.Members.ContainsKey(id)) {
            var oldMembers=quizData.Members.Keys.Where(k=>k.StartsWith("name:") &&
                (NormalizeScoreName(k.Substring(5))==NormalizeScoreName(name) ||
                 (quizData.Scores.ContainsKey(k)&&NormalizeScoreName(quizData.Scores[k].UserName)==NormalizeScoreName(name)))).ToList();
            Require(oldMembers.Count<=1,"Niejednoznaczne stare przypisanie drużyny.");
            if(oldMembers.Count==1){quizData.Members[id]=quizData.Members[oldMembers[0]];quizData.Members.Remove(oldMembers[0]);}
        }
        string oid=r.Question.Options[index-1].Id;
        if(r.Votes.TryGetValue(id,out v)) {
            if(v.OptionId==oid)return;
            if(r.Question.Policy=="FIRST" || (r.Question.Policy=="LIMITED"&&v.Changes>=r.Question.ChangeLimit))return;
            v.Changes++;
        } else {Require(r.Votes.Count<25000,"Limit głosów.");v=new Vote();r.Votes[id]=v;}
        v.OptionId=oid;v.Name=name;v.ElapsedMs=Elapsed(r);v.TeamId=quizData.Settings.TeamMode&&quizData.Members.ContainsKey(id)?quizData.Members[id]:"";
        quizData.Revision++;SaveState();stateBroadcastPending=true;if(Now()-lastBroadcastAt>=100)BroadcastState();
    }
    private void ModerateVote() {
        var r=quizData.Current;Require(r!=null&&(r.Status=="open"||r.Status=="locked"),"Korekta wymaga otwartej/zamkniętej rundy przed reveal.");
        string id=GetStringArg("userId");Require(r.Votes.ContainsKey(id),"Nie ma głosu.");int index=GetIntArg("answerIndex",0);
        if(index==0)r.Votes.Remove(id);else {ValidateRange(index,1,r.Question.Options.Count,"Wybór");r.Votes[id].OptionId=r.Question.Options[index-1].Id;r.Votes[id].ElapsedMs=Elapsed(r);r.Votes[id].Moderated=true;}
    }
    private void TimerOperation(string op) {
        var r=quizData.Current;Require(r!=null&&r.Status=="open"&&r.Question.Duration>0,"Timer automatyczny wymaga otwartej rundy.");
        long now=Now();
        if(op=="pause") {if(!r.Paused){r.ElapsedMs=Elapsed(r);r.ClockAt=now;r.RemainingMs=Math.Max(0,r.ClosesAt-now);r.ClosesAt=0;r.Paused=true;}}
        else if(op=="resume") {if(r.Paused){r.Paused=false;r.ClockAt=now;r.ClosesAt=now+r.RemainingMs;r.RemainingMs=0;}}
        else {int delta=GetIntArg("seconds",0);ValidateRange(delta,-3600,3600,"Zmiana czasu");if(r.Paused)r.RemainingMs=Math.Max(0,Math.Min(3600000,r.RemainingMs+delta*1000L));else r.ClosesAt=now+Math.Max(0,Math.Min(3600000,r.ClosesAt-now+delta*1000L));}
    }
    private bool AllowCommand(string command,string user) {
        if(!quizData.Settings.Cooldowns||IsChatAdmin())return true;long now=Now();long until;
        string key=command+":"+user;string global=command+":*";
        if((commandCooldowns.TryGetValue(key,out until)&&now<until)||(commandCooldowns.TryGetValue(global,out until)&&now<until))return false;
        if(commandCooldowns.Count>30000)foreach(string stale in commandCooldowns.Where(x=>x.Value<=now).Select(x=>x.Key).ToList())commandCooldowns.Remove(stale);
        commandCooldowns[key]=now+quizData.Settings.UserCooldown*1000L;commandCooldowns[global]=now+quizData.Settings.GlobalCooldown*1000L;return true;
    }
    private void HandleTwitchChat() {
        if(GetBoolArg("isInternal"))return;string message=GetStringArg("message").Trim();if(message.Length>1000)return;
        int answer;if(TryParseVote(message,quizData.Settings.VoteParsing,quizData.Current==null?12:quizData.Current.Question.Options.Count,out answer)){RegisterVote(answer);return;}
        string id=GetStringArg("userId");if(id=="")id="name:"+GetStringArg("userName").ToLowerInvariant();
        if(message.Equals(quizData.Settings.PointsCommand,StringComparison.OrdinalIgnoreCase)&&AllowCommand("points",id)){SendUserPoints();return;}
        if(message.Equals(quizData.Settings.RankingCommand,StringComparison.OrdinalIgnoreCase)&&AllowCommand("ranking",id)){SendRanking();return;}
        string adminCommand=quizData.Settings.AdminCommand;
        if(message.StartsWith(adminCommand,StringComparison.OrdinalIgnoreCase)&&(message.Length==adminCommand.Length||Char.IsWhiteSpace(message[adminCommand.Length]))&&IsChatAdmin())HandleAdminChatCommand(message);
    }
    private static bool TryParseVote(string raw,string mode,int answerCount,out int answer) {
        answer=0;if(raw==null||raw.Length>1000||answerCount<1||answerCount>12)return false;
        string message=raw.Trim();
        if(message.Length<2||message[0]!='!'||message[1]<'1'||message[1]>'9')return false;
        for(int i=0;i<message.Length;i++) {
            char c=message[i];if(Char.IsLowSurrogate(c)||Char.IsHighSurrogate(c)&&(i+1>=message.Length||!Char.IsLowSurrogate(message[i+1])))return false;
            var category=System.Globalization.CharUnicodeInfo.GetUnicodeCategory(message,i);
            if(category==System.Globalization.UnicodeCategory.Format||(category==System.Globalization.UnicodeCategory.Control&&!Char.IsWhiteSpace(c)))return false;
            if(Char.IsHighSurrogate(c))i++;
        }
        int end=1;while(end<message.Length&&message[end]>='0'&&message[end]<='9')end++;
        if(end>3||!Int32.TryParse(message.Substring(1,end-1),out answer)||answer>answerCount||answer>12){answer=0;return false;}
        if(end<message.Length&&System.Globalization.CharUnicodeInfo.GetUnicodeCategory(message,end)==System.Globalization.UnicodeCategory.DecimalDigitNumber){answer=0;return false;}
        bool accepted=mode=="STRICT"?end==message.Length:mode=="WHITESPACE"?(end==message.Length||Char.IsWhiteSpace(message[end])):mode=="ATTACHED";
        if(!accepted)answer=0;return accepted;
    }
    private void SendUserPoints() {
        string id=GetStringArg("userId");if(id=="")id="name:"+GetStringArg("userName").ToLowerInvariant();var scores=quizData.Scores;ScoreEntry e;
        if(!scores.TryGetValue(id,out e)){string key;if(TryFindScoreKeyByName(scores,GetDisplayName(),out key))e=scores[key];}
        SendChat(GetDisplayName()+": "+(e==null?"0 pkt":e.Points+" pkt | poprawne "+e.CorrectAnswers+"/"+e.AnsweredQuestions));
    }
    private void SendRanking() {var top=SortScores(quizData.Scores).Take(5).Select((x,i)=>(i+1)+". "+SafeName(x.UserName)+" - "+x.Points+" pkt");SendChat("Quiz TOP 5: "+String.Join(" | ",top));}
    private void SendChat(string text) {text=(text??"").Replace("\r"," ").Replace("\n"," ");if(text.Length>470)text=text.Substring(0,469)+"…";CPH.SendMessage(text,true,true);}
    private string SafeName(string text){text=(text??"").Replace("\r"," ").Replace("\n"," ").Trim();return text==""?"Widz":text.Substring(0,Math.Min(80,text.Length));}
    private long UnixMilliseconds(){return Now();}
    private void HandlePanelOperation(string op) {
        if(op=="sync"||op=="tick"){BroadcastState();return;}
        if(op=="getscores"){BroadcastScores();return;}
        if(op=="getdata"){BroadcastData();return;}
        if(op=="getcompetitive"){BroadcastCompetitive();return;}

        if(op=="diagnostics"){Broadcast(new{type="quiz-diagnostics",requestId=requestId,appVersion=AppVersion,build=BuildLabel,enginePatch=EnginePatch,protocol=Protocol,stateSchema=2,backupSchema=1,rankingArchiveSchema=1,rankingArchiveManagement=true,archiveCleanup=true,openArchiveFolder=true,migration=quizData.Migration,aggregate="PARTIAL",autoFinalizeError=CurrentExpirationError(),rankingCount=quizData.Scores.Count,historyCount=quizData.History.Count,snapshotCount=LoadSnapshots().Count,revision=quizData.Revision});return;}
        if(op=="start"){if(GetStringArg("payloadJson")!="")Start(Read<Question>(GetStringArg("payloadJson")));else StartQuiz(GetStringArg("question"),GetStringArg("answersJson"),GetIntArg("durationSeconds",0),true);return;}
        if(op=="lock"||op=="lockexpired"){LockQuiz(op=="lockexpired",true);return;}
        if(op=="reopen"){ReopenQuiz(true);return;}
        if(op=="reveal") {
            Require(quizData.Current!=null,"Brak rundy.");
            if(GetStringArg("payloadJson")!=""){Reveal(quizData.Current,Read<Question>(GetStringArg("payloadJson")));quizData.Revision++;SaveState();BroadcastState();BroadcastScores();AnnounceRevealedRound(quizData.Current);}
            else RevealAnswer(GetIntArg("correctIndex",0),GetIntArg("pointsPerCorrect",1),GetIntArg("pointsPerAdjacent",0),GetStringArg("scoringMode"),GetStringArg("partialIndicesJson"),true);return;
        }
        if(op=="cancel"){CancelQuiz(true);return;}
        if(op=="hide"||op=="show"){SetOverlayVisibility(op=="show");return;}
        if(op=="importscores"){EnsureRoundFinished();ImportScoresFromPanel(GetStringArg("scoresJson"),GetStringArg("importMode"),GetStringArg("confirmText"));BroadcastData();return;}
        if(op=="exportbackup"){EnsureRoundFinished();Broadcast(new{type="quiz-download",kind="backup",envelope=new Backup{Data=Copy(quizData),CreatedAt=Now()}});return;}
        if(op=="exportbank"){Broadcast(new{type="quiz-download",kind="bank",envelope=new BankEnvelope{Questions=Copy(quizData.Bank),Sets=Copy(quizData.Sets),CreatedAt=Now()}});return;}
        if(op=="previewbackup"||op=="previewbank") {
            if(op=="previewbackup") {var b=Read<Backup>(GetStringArg("payloadJson"));ValidateBackup(b);Broadcast(new{type="quiz-preview",kind="backup",users=b.Data.Scores.Count,questions=b.Data.Bank.Count,sets=b.Data.Sets.Count,history=b.Data.History.Count});}
            else {var b=Read<BankEnvelope>(GetStringArg("payloadJson"));ValidateBank(b);Broadcast(new{type="quiz-preview",kind="bank",questions=b.Questions.Count,sets=b.Sets.Count});}return;
        }
        switch(op) {
        case "competitive": {
            EnsureRoundFinished();var settings=Read<CompetitiveSettings>(GetStringArg("payloadJson"));ValidateCompetitiveSettings(settings);
            settings.SeasonsEnabled=quizData.Competitive.Settings.SeasonsEnabled;quizData.Competitive.Settings=settings;break;
        }

        case "preparenext":PrepareNext();break;
        case "pause": case "resume": case "time": TimerOperation(op);Expire();break;
        case "moderatevote":ModerateVote();break;
        case "undo":Undo();break;
        case "rescore":Rescore();break;
        case "resetscores":
            ResetScoresWithArchive(); return;
        case "setscore": {
            string id=GetStringArg("userId"); Require(quizData.Scores.ContainsKey(id),"Nie znaleziono widza."); int p=GetIntArg("newPoints",-1); ValidateRange(p,0,100000000,"Korekta");
            Snapshot("manual score correction"); var scores=Copy(quizData.Scores); scores[id].Points=p; scores[id].LastUpdatedAt=Now(); NewEpoch(scores); break;
        }
        case "restoreimportbackup":EnsureRoundFinished();Confirm("COFNIJ");Require(quizData.HasImportBackup,"Brak kopii importu.");Snapshot("undo CSV import");NewEpoch(Copy(quizData.ImportBackup));quizData.ImportBackup.Clear();quizData.HasImportBackup=false;break;
        case "settings": {
            string settingsJson=GetStringArg("payloadJson");var nextSettings=Read<Settings>(settingsJson);var providedSettings=JObject.Parse(settingsJson);
            if(!providedSettings.Properties().Any(property=>String.Equals(property.Name,"PointsCommand",StringComparison.OrdinalIgnoreCase)))nextSettings.PointsCommand=quizData.Settings.PointsCommand;
            if(!providedSettings.Properties().Any(property=>String.Equals(property.Name,"RankingCommand",StringComparison.OrdinalIgnoreCase)))nextSettings.RankingCommand=quizData.Settings.RankingCommand;
            if(!providedSettings.Properties().Any(property=>String.Equals(property.Name,"AdminCommand",StringComparison.OrdinalIgnoreCase)))nextSettings.AdminCommand=quizData.Settings.AdminCommand;
            if(!providedSettings.Properties().Any(property=>String.Equals(property.Name,"AnnounceRoundEventsInChat",StringComparison.OrdinalIgnoreCase)))nextSettings.AnnounceRoundEventsInChat=quizData.Settings.AnnounceRoundEventsInChat;
            ValidateSettings(nextSettings);
            var oldPolicy=quizData.Settings.ArchiveRetention;var newPolicy=nextSettings.ArchiveRetention;
            if(newPolicy.Enabled&&(!oldPolicy.Enabled||newPolicy.MaxCount<oldPolicy.MaxCount||newPolicy.MaxAgeDays<oldPolicy.MaxAgeDays||newPolicy.IncludeManual&&!oldPolicy.IncludeManual||newPolicy.IncludeSafety&&!oldPolicy.IncludeSafety))Confirm("USUN ARCHIWA");
            if(nextSettings.TeamMode!=quizData.Settings.TeamMode)EnsureRoundFinished();quizData.Settings=nextSettings;break;
        }
        case "savedraft":quizData.Draft=Read<Question>(GetStringArg("payloadJson"));ValidateQuestion(quizData.Draft,true);break;
        case "savequestion":case "savetemplate": {
            var q=Read<Question>(GetStringArg("payloadJson"));ValidateQuestion(q);var list=quizData.Bank;
            int index=list.FindIndex(x=>x.Id==q.Id);if(index<0)list.Add(q);else list[index]=q;break;
        }
        case "deletequestion":case "deletetemplate": {
            var list=quizData.Bank;string id=GetStringArg("itemId");Require(list.Any(x=>x.Id==id),"Nie ma szablonu.");list.RemoveAll(x=>x.Id==id);break;
        }
        case "duplicatequestion":case "duplicatetemplate": {
            var list=quizData.Bank;var q=list.FirstOrDefault(x=>x.Id==GetStringArg("itemId"));Require(q!=null,"Nie ma szablonu.");q=Copy(q);q.Id=Id();q.Name=(q.Name+" (kopia)").Substring(0,Math.Min(80,q.Name.Length+8));list.Add(q);break;
        }
        case "saveset": {
            var set=Read<QuizSet>(GetStringArg("payloadJson"));var temp=new Root();temp.Sets.Add(set);Validate(temp);int i=quizData.Sets.FindIndex(x=>x.Id==set.Id);if(i<0)quizData.Sets.Add(set);else quizData.Sets[i]=set;break;
        }
        case "deleteset":quizData.Sets.RemoveAll(x=>x.Id==GetStringArg("itemId"));break;
        case "loadset": {
            EnsureRoundFinished();var set=quizData.Sets.FirstOrDefault(x=>x.Id==GetStringArg("itemId"));Require(set!=null&&set.Questions.Count>0,"Pusty/nieznany zestaw.");
            quizData.Queue=Copy(set.Questions);quizData.QueueSetId=set.Id;quizData.QueueIndex=-1;quizData.PreparedQueueIndex=-1;
            if(GetBoolArg("shuffle")){var random=new Random();quizData.Queue=quizData.Queue.OrderBy(x=>random.Next()).ToList();}quizData.Podium=false;break;
        }
        case "next":case "previous": {
            EnsureRoundFinished();Require(quizData.Queue!=null&&quizData.Queue.Count>0,"Najpierw wczytaj zestaw.");
            int n=op=="next"&&quizData.PreparedQueueIndex==quizData.QueueIndex&&quizData.PreparedQueueIndex>=0?quizData.QueueIndex:quizData.QueueIndex+(op=="next"?1:-1);Require(n>=0,"Początek kolejki.");
            if(n>=quizData.Queue.Count){quizData.Podium=quizData.Settings.Overlay.Podium;quizData.OverlayVisible=true;quizData.QueueIndex=quizData.Queue.Count;break;}
            quizData.QueueIndex=n;Start(quizData.Queue[n]);return;
        }
        case "podium":EnsureRoundFinished();Require(quizData.Settings.Overlay.Podium,"Podium jest wyłączone w ustawieniach.");quizData.Podium=!quizData.Podium;if(quizData.Podium)quizData.OverlayVisible=true;break;
        case "teams":EnsureRoundFinished();var teams=Read<TeamEnvelope>(GetStringArg("payloadJson"));Require(teams!=null,"Puste drużyny.");quizData.Teams=teams.Teams;quizData.Members=teams.Members;break;
        case "restorebackup": {
            EnsureRoundFinished();Confirm("RESTORE");var b=Read<Backup>(GetStringArg("payloadJson"));ValidateBackup(b);CheckCompetitiveRestore(b.Data);
            string sourceRaw=storedStateJson;Snapshot("full backup restore",sourceRaw);
            Require(LoadSnapshots().Any(x=>x.Reason=="full backup restore"&&String.Equals(x.Data,sourceRaw,StringComparison.Ordinal)),"Nie potwierdzono zapisu snapshotu. Dane pozostają bez zmian.");
            int rev=quizData.Revision;quizData=Copy(b.Data);ConsolidateLegacyTemplates(quizData);quizData.Revision=Math.Max(rev,quizData.Revision);SanitizeRestoredRound();break;
        }
        case "restoresnapshot": {
            EnsureRoundFinished();Confirm("RESTORE");var s=LoadSnapshots().FirstOrDefault(x=>x.Id==GetStringArg("itemId"));Require(s!=null,"Snapshot nie istnieje.");
            var restored=Read<Root>(s.Data);ValidateBackup(new Backup{Data=restored,CreatedAt=s.At});CheckCompetitiveRestore(restored);
            string sourceRaw=storedStateJson;Snapshot("before snapshot restore",sourceRaw);
            Require(LoadSnapshots().Any(x=>x.Reason=="before snapshot restore"&&String.Equals(x.Data,sourceRaw,StringComparison.Ordinal)),"Nie potwierdzono zapisu snapshotu. Dane pozostają bez zmian.");
            int rev=quizData.Revision;quizData=restored;ConsolidateLegacyTemplates(quizData);quizData.Revision=Math.Max(rev,quizData.Revision);SanitizeRestoredRound();break;
        }
        case "importbank": {
            EnsureRoundFinished();Confirm("IMPORT");var b=Read<BankEnvelope>(GetStringArg("payloadJson"));ValidateBank(b);
            string mode=GetStringArg("importMode");Require(mode=="replace"||mode=="merge","Tryb importu szablonów.");Snapshot("Szablony import "+mode);
            if(mode=="replace"){quizData.Bank=Copy(b.Questions);quizData.Sets=Copy(b.Sets);}else {
                foreach(var q in b.Questions){int i=quizData.Bank.FindIndex(x=>x.Id==q.Id);if(i<0)quizData.Bank.Add(q);else quizData.Bank[i]=Copy(q);}
                foreach(var s in b.Sets){int i=quizData.Sets.FindIndex(x=>x.Id==s.Id);if(i<0)quizData.Sets.Add(s);else quizData.Sets[i]=Copy(s);}
            }break;
        }
        case "migratebrowser": {
            if(quizData.BrowserMigrated)break;var b=Read<BrowserEnvelope>(GetStringArg("payloadJson"));Require(b!=null&&b.Templates!=null&&b.Templates.Count<=100,"Browser migration.");
            Snapshot("browser draft/templates migration");foreach(var q in b.Templates)AddLegacyTemplateToBank(quizData.Bank,q);quizData.Templates.Clear();
            if(b.Draft!=null){ValidateQuestion(b.Draft,true);if(quizData.Draft==null)quizData.Draft=b.Draft;}quizData.BrowserMigrated=true;break;
        }
        default:throw new InvalidOperationException("Nieznana operacja: "+op);
        }
        quizData.Revision++;SaveState();BroadcastState();BroadcastScores();BroadcastData();
        if((op=="pause"||op=="resume"||op=="time")&&quizData.Current!=null&&quizData.Current.Status=="revealed")AnnounceRevealedRound(quizData.Current);
        if(op=="resetscores") { CPH.LogInfo("[LukiQuiz V2] RESET rankingu zapisany, revision="+quizData.Revision+", scores="+quizData.Scores.Count);  }
        else if(op=="setscore") { CPH.LogInfo("[LukiQuiz V2] Korekta punktów zapisana, revision="+quizData.Revision);  }
        else if(op=="savequestion"||op=="savetemplate")BroadcastNotice("Szablon zapisany.");
        else if(op=="importbank")BroadcastNotice("Szablony zostały zaimportowane.");
    }
    private void ValidateBackup(Backup b) {Require(b!=null&&b.Format=="LukiQuizBackup"&&b.Schema==1&&b.Data!=null,"Nieobsługiwany backup.");ValidateText(b.AppVersion,1,30,"AppVersion");ValidateRange(b.CreatedAt,0,4102444800000L,"Backup timestamp");Validate(b.Data);var copy=Copy(b.Data);var previous=quizData;try{quizData=copy;Replay();Require(JsonConvert.SerializeObject(copy.Scores)==JsonConvert.SerializeObject(b.Data.Scores)&&JsonConvert.SerializeObject(copy.TeamScores)==JsonConvert.SerializeObject(b.Data.TeamScores)&&JsonConvert.SerializeObject(copy.Competitive.Stats)==JsonConvert.SerializeObject(b.Data.Competitive.Stats),"Ranking backupu nie zgadza się z historią.");}finally{quizData=previous;}}
    private void ValidateBank(BankEnvelope b) {Require(b!=null&&b.Format=="LukiQuizBank"&&b.Schema==1&&b.Questions!=null&&b.Sets!=null,"Nieobsługiwany plik Szablonów.");ValidateText(b.AppVersion,1,30,"AppVersion");ValidateRange(b.CreatedAt,0,4102444800000L,"Bank timestamp");var d=new Root();d.Bank=b.Questions;d.Sets=b.Sets;Validate(d);}
    private void SanitizeRestoredRound() {if(quizData.Current!=null&&(quizData.Current.Status=="open"||quizData.Current.Status=="locked")){quizData.Current.Status="cancelled";quizData.Current.ClosesAt=0;quizData.Current.Paused=false;quizData.Current.Visible=false;}quizData.Podium=false;}
    private void Broadcast(object obj) {
        var message=JObject.FromObject(obj,JsonSerializer.Create(serializerOptions));
        if(requestId!="") {
            message["requestId"]=requestId;message["build"]=BuildLabel;
            if(message.Property("operation")==null)message["operation"]=requestOperation;
            if(message.Property("revision")==null)message["revision"]=quizData==null?0:quizData.Revision;
        }
        CPH.WebsocketBroadcastJson(message.ToString(Formatting.None));
    }
    private void BroadcastError(string text) {requestFailed=true;Broadcast(new{type="quiz-error",requestId=requestId,build=BuildLabel,message=text});CPH.LogWarn("[LukiQuiz V2] request="+requestId+" "+text);}
    private void BroadcastNotice(string text) {Broadcast(new{type="quiz-notice",message=text});}
    private object PublicState() {
        var r=quizData.Current;bool reveal=r!=null&&r.Status=="revealed";
        bool distribution=r==null||reveal||r.Question.Voting=="LIVE";bool counts=r==null||reveal||r.Question.Voting!="HIDDEN";
        var options=r==null?new List<Option>():r.Question.Options;
        return new {
            type="quiz-state",appVersion=AppVersion,version=2,revision=quizData.Revision,serverNow=Now(),status=r==null?"idle":r.Status,
            language=quizData.Settings.SyncLanguage?quizData.Settings.PanelLanguage:quizData.Settings.OverlayLanguage,panelLanguage=quizData.Settings.PanelLanguage,syncLanguage=quizData.Settings.SyncLanguage,
            visible=quizData.OverlayVisible&&(quizData.Podium||(r!=null&&r.Visible)),question=r==null?"":r.Question.Text,roundId=r==null?"":r.Id,mode=r==null?"QUIZ":r.Question.Mode,
            voting=r==null?"LIVE":r.Question.Voting,totalVotes=counts&&r!=null?r.Votes.Count:0,distribution=distribution,countsVisible=counts,
            options=options.Select((o,i)=>new{index=i+1,id=o.Id,text=o.Text,count=distribution?r.Votes.Count(v=>v.Value.OptionId==o.Id):0,correct=reveal&&o.Correct,partial=reveal&&!o.Correct&&o.Partial,points=reveal&&r.Question.Mode=="QUIZ"?(o.Points.HasValue?o.Points.Value:o.Correct?r.Question.FullPoints:o.Partial?r.Question.PartialPoints:0):0}).ToList(),
            closesAt=r!=null?r.ClosesAt:0,paused=r!=null&&r.Paused,remainingMs=r!=null?r.RemainingMs:0,
            leaderboard=SortScores(quizData.Scores).Take(10).Select((e,i)=>new{position=i+1,userId=e.UserId,userName=SafeName(e.UserName),points=e.Points,correct=e.CorrectAnswers,answered=e.AnsweredQuestions}).ToList(),
            overlay=quizData.Settings.Overlay,podium=quizData.Podium&&quizData.Settings.Overlay.Podium,
            teamMode=quizData.Settings.TeamMode,teams=quizData.Teams.Select(t=>new{id=t.Id,name=t.Name,points=quizData.TeamScores.ContainsKey(t.Id)?quizData.TeamScores[t.Id]:0}).OrderByDescending(t=>t.points).ToList()
        };
    }
    private void RemoveRetiredRoundFields(JObject round) {
        if(round==null)return;var competitive=round["Competitive"] as JObject;if(competitive==null)return;
        competitive.Remove("SeasonId");var settings=competitive["Settings"] as JObject;if(settings!=null)settings.Remove("SeasonsEnabled");
    }
    private JObject PanelPayload(object value) {
        if(value==null)return null;var payload=JObject.FromObject(value,JsonSerializer.Create(serializerOptions));
        if(value is Root) {
            var competitive=payload["Competitive"] as JObject;
            if(competitive!=null) {
                competitive.Remove("Seasons");competitive.Remove("ActiveSeasonId");competitive.Remove("FrozenRounds");
                var settings=competitive["Settings"] as JObject;if(settings!=null)settings.Remove("SeasonsEnabled");
                foreach(string key in new[]{"Base","Stats"}){var projection=competitive[key] as JObject;if(projection!=null)projection.Remove("SeasonScores");}
            }
            var history=payload["History"] as JArray;if(history!=null)foreach(var round in history.OfType<JObject>())RemoveRetiredRoundFields(round);
            RemoveRetiredRoundFields(payload["Current"] as JObject);
        } else if(value is Round)RemoveRetiredRoundFields(payload);
        return payload;
    }
    private void BroadcastState() {
        lastBroadcastAt=Now();stateBroadcastPending=false;var p=PublicState();CPH.SetGlobalVar(PublicVariable,JsonConvert.SerializeObject(p),false);Broadcast(p);
        Broadcast(new{type="quiz-admin",appVersion=AppVersion,revision=quizData.Revision,current=PanelPayload(quizData.Current),epoch=quizData.Epoch,queueIndex=quizData.QueueIndex,queueCount=quizData.Queue==null?0:quizData.Queue.Count});
    }
    private object ScorePayload() { return SortScores(quizData.Scores).Select((e,i)=>new{position=i+1,userId=e.UserId,userName=SafeName(e.UserName),points=e.Points,correct=e.CorrectAnswers,answered=e.AnsweredQuestions,lastUpdatedAt=e.LastUpdatedAt}).ToList(); }
    private void BroadcastScores() {Broadcast(new{type="quiz-scores",appVersion=AppVersion,revision=quizData.Revision,scores=ScorePayload()});}
    private void BroadcastData() {
        object snapshotInfo;
        try { snapshotInfo=LoadSnapshots().Select(s=>new{id=s.Id,at=s.At,reason=s.Reason,bytes=Encoding.UTF8.GetByteCount(s.Data)}).ToList(); }
        catch(Exception ex) { CPH.LogWarn("[LukiQuiz V2] Lista snapshotów niedostępna dla panelu: "+ex.GetType().Name); snapshotInfo=new object[0]; }
        Broadcast(new{type="quiz-data",appVersion=AppVersion,revision=quizData.Revision,data=PanelPayload(quizData),snapshots=snapshotInfo,competitiveDefinitions=AchievementDefinitions()});
    }
    private bool IsChatAdmin()
    {
        if (GetBoolArg("isBroadcaster") ||
            GetBoolArg("isModerator") ||
            GetBoolArg("isMod"))
        {
            return true;
        }

        string userId = GetStringArg("userId");
        string broadcasterId = GetStringArg("broadcastUserId");
        return !String.IsNullOrWhiteSpace(userId) &&
               !String.IsNullOrWhiteSpace(broadcasterId) &&
               String.Equals(userId, broadcasterId, StringComparison.OrdinalIgnoreCase);
    }

    private string GetDisplayName()
    {
        string displayName = GetStringArg("user");
        if (String.IsNullOrWhiteSpace(displayName))
        {
            displayName = GetStringArg("userName");
        }
        return SafeName(displayName);
    }

    private string GetStringArg(string name) { string value; return requestArgs.TryGetValue(name,out value) ? value ?? "" : ""; }
    private int GetIntArg(string name,int fallback) { int value; return Int32.TryParse(GetStringArg(name),out value)?value:fallback; }
    private bool GetBoolArg(string name) { bool value; return Boolean.TryParse(GetStringArg(name),out value)&&value; }

    private void HandleAdminChatCommand(string message)
    {
        string adminCommand=quizData.Settings.AdminCommand;
        string rest = message.Length > adminCommand.Length
            ? message.Substring(adminCommand.Length).Trim()
            : "";
        if (String.IsNullOrWhiteSpace(rest) ||
            String.Equals(rest, "pomoc", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(rest, "help", StringComparison.OrdinalIgnoreCase))
        {
            SendChat(
                "Quiz admin: " + adminCommand +
                " start Pytanie | Odp. 1 | Odp. 2 ... - " + adminCommand +
                " zamknij - " + adminCommand +
                " wynik NUMER [PEŁNE PUNKTY] [PUNKTY ZA ODPOWIEDŹ OBOK] - " +
                adminCommand + " anuluj - " + adminCommand + " pokaz/ukryj");
            return;
        }

        if (rest.StartsWith("start ", StringComparison.OrdinalIgnoreCase))
        {
            string definition = rest.Substring(6).Trim();
            string[] parts = definition
                .Split(new[] { '|' }, StringSplitOptions.None)
                .Select(x => x.Trim())
                .Where(x => !String.IsNullOrWhiteSpace(x))
                .ToArray();

            if (parts.Length < 3)
            {
                SendChat(
                    "Quiz: użycie: " + adminCommand +
                    " start Pytanie | Odpowiedź 1 | Odpowiedź 2 | ...");
                return;
            }

            string question = parts[0];
            string answersJson = JsonConvert.SerializeObject(parts.Skip(1).ToList());
            StartQuiz(question, answersJson, 0, true);
            return;
        }

        if (String.Equals(rest, "zamknij", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(rest, "stop", StringComparison.OrdinalIgnoreCase))
        {
            LockQuiz(false, true);
            return;
        }

        if (rest.StartsWith("wynik ", StringComparison.OrdinalIgnoreCase) ||
            rest.StartsWith("odpowiedz ", StringComparison.OrdinalIgnoreCase) ||
            rest.StartsWith("odpowiedź ", StringComparison.OrdinalIgnoreCase))
        {
            string[] words = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int correctIndex = 0;
            int points = 1;
            int adjacentPoints = 0;
            if (words.Length >= 2)
            {
                Int32.TryParse(words[1], out correctIndex);
            }
            if (words.Length >= 3)
            {
                Int32.TryParse(words[2], out points);
            }
            if (words.Length >= 4)
            {
                Int32.TryParse(words[3], out adjacentPoints);
            }

  
            RevealAnswer(
                correctIndex,
                points,
                adjacentPoints,
                "adjacent",
                "[]",
                true);
            return;
        }

        if (String.Equals(rest, "anuluj", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(rest, "cancel", StringComparison.OrdinalIgnoreCase))
        {
            CancelQuiz(true);
            return;
        }

        if (String.Equals(rest, "pokaz", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(rest, "pokaż", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(rest, "show", StringComparison.OrdinalIgnoreCase))
        {
            SetOverlayVisibility(true);
            return;
        }

        if (String.Equals(rest, "ukryj", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(rest, "hide", StringComparison.OrdinalIgnoreCase))
        {
            SetOverlayVisibility(false);
            return;
        }

        SendChat("Quiz: nieznana komenda. Wpisz " + adminCommand + " pomoc");
    }

    private void ImportScoresFromPanel(
        string scoresJson,
        string importMode,
        string confirmText)
    {
        scoresJson = scoresJson ?? "";
        if (String.IsNullOrWhiteSpace(scoresJson))
        {
            BroadcastError("Plik importu nie zawiera żadnych wpisów rankingu.");
            return;
        }

        if (scoresJson.Length > MaxImportedPayloadLength)
        {
            BroadcastError("Plik importu jest zbyt duży.");
            return;
        }

        string mode = String.Equals(
            importMode,
            "merge",
            StringComparison.OrdinalIgnoreCase)
                ? "merge"
                : "replace";
        string requiredConfirmation =
            mode == "merge" ? "IMPORT_MERGE" : "IMPORT_REPLACE";
        if (!String.Equals(
                confirmText,
                requiredConfirmation,
                StringComparison.Ordinal))
        {
            BroadcastError("Import rankingu został odrzucony: brak potwierdzenia.");
            return;
        }

        List<ImportedScoreEntry> importedRows;
        try
        {
            importedRows =
                Read<List<ImportedScoreEntry>>(scoresJson);
        }
        catch (Exception ex)
        {
            CPH.LogWarn("[LocalQuiz] Nieprawidłowy JSON importu: " + ex.GetType().Name);
            BroadcastError("Nie udało się odczytać danych z pliku CSV.");
            return;
        }

        if (importedRows == null || importedRows.Count == 0)
        {
            BroadcastError("Plik importu nie zawiera żadnych wpisów rankingu.");
            return;
        }

        if (importedRows.Count > MaxImportedScores)
        {
            BroadcastError(
                "Plik zawiera zbyt wiele wpisów. Maksimum jednego importu to " +
                MaxImportedScores + ".");
            return;
        }

        long now = UnixMilliseconds();
        List<PreparedImportedScore> preparedRows =
            new List<PreparedImportedScore>();
        HashSet<string> importedIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> importedNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < importedRows.Count; index++)
        {
            ImportedScoreEntry source = importedRows[index];
            int rowNumber = index + 2;
            if (source == null)
            {
                BroadcastError("Wiersz " + rowNumber + " importu jest pusty.");
                return;
            }

            string userName = (source.UserName ?? "")
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();
            if (String.IsNullOrWhiteSpace(userName))
            {
                BroadcastError(
                    "Wiersz " + rowNumber + ": brakuje nazwy użytkownika.");
                return;
            }
            if (userName.Length > 80 ||
                userName.Any(character => Char.IsControl(character)) ||
                HasSpreadsheetFormulaPrefix(userName))
            {
                BroadcastError(
                    "Wiersz " + rowNumber + ": nazwa użytkownika jest nieprawidłowa.");
                return;
            }

            string normalizedName = NormalizeScoreName(userName);
            if (!importedNames.Add(normalizedName))
            {
                BroadcastError(
                    "Plik zawiera więcej niż jeden wpis użytkownika " +
                    SafeName(userName) + ".");
                return;
            }

            string userId = (source.UserId ?? "").Trim();
            bool hasExplicitUserId = !String.IsNullOrWhiteSpace(userId);
            if (hasExplicitUserId)
            {
                if (userId.Length > MaxUserIdLength ||
                    userId.Any(character => Char.IsControl(character)) ||
                    HasSpreadsheetFormulaPrefix(userId))
                {
                    BroadcastError(
                        "Wiersz " + rowNumber + ": ID użytkownika jest nieprawidłowe.");
                    return;
                }
            }
            else
            {
                userId = BuildNameScoreId(userName);
            }

            if (!importedIds.Add(userId))
            {
                BroadcastError(
                    "Plik zawiera zduplikowane ID użytkownika w wierszu " +
                    rowNumber + ".");
                return;
            }

            if (source.Points < 0 || source.Points > 100000000 ||
                source.CorrectAnswers < 0 ||
                source.CorrectAnswers > 100000000 ||
                source.AnsweredQuestions < 0 ||
                source.AnsweredQuestions > 100000000)
            {
                BroadcastError(
                    "Wiersz " + rowNumber +
                    ": statystyki muszą być nieujemnymi liczbami całkowitymi.");
                return;
            }

            if (source.CorrectAnswers > source.AnsweredQuestions)
            {
                BroadcastError(
                    "Wiersz " + rowNumber +
                    ": liczba poprawnych nie może przekraczać odpowiedzianych.");
                return;
            }

            ScoreEntry entry = new ScoreEntry();
            entry.UserId = userId;
            entry.UserName = userName;
            entry.Points = source.Points;
            entry.CorrectAnswers = source.CorrectAnswers;
            entry.AnsweredQuestions = source.AnsweredQuestions;
            entry.LastUpdatedAt =
                source.LastUpdatedAt > 0 && source.LastUpdatedAt <= now + 86400000L
                    ? source.LastUpdatedAt
                    : now;

            PreparedImportedScore prepared = new PreparedImportedScore();
            prepared.Entry = entry;
            prepared.HasExplicitUserId = hasExplicitUserId;
            preparedRows.Add(prepared);
        }

        Dictionary<string, ScoreEntry> previousScores = LoadScores();
        Dictionary<string, ScoreEntry> result;

        if (mode == "merge")
        {
            result = new Dictionary<string, ScoreEntry>(
                previousScores,
                StringComparer.OrdinalIgnoreCase);

            foreach (PreparedImportedScore prepared in preparedRows)
            {
                ScoreEntry imported = prepared.Entry;
                string targetKey = imported.UserId;
                string matchingKey;
                if (!result.ContainsKey(targetKey) &&
                    TryFindImportKey(result, imported.UserName, prepared.HasExplicitUserId, out matchingKey))
                {
                    ScoreEntry existing = result[matchingKey];
                    if (!prepared.HasExplicitUserId &&
                        existing != null &&
                        !String.IsNullOrWhiteSpace(existing.UserId))
                    {
                        targetKey = matchingKey;
                        imported.UserId = existing.UserId;
                    }
                    else if (!String.Equals(
                                 matchingKey,
                                 targetKey,
                                 StringComparison.OrdinalIgnoreCase))
                    {
                        result.Remove(matchingKey);
                    }
                }

                result[targetKey] = imported;
            }
        }
        else
        {
            result =
                new Dictionary<string, ScoreEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (PreparedImportedScore prepared in preparedRows)
            {
                result[prepared.Entry.UserId] = prepared.Entry;
            }
        }

        Snapshot("CSV import " + mode);
        quizData.ImportBackup = Copy(previousScores);
        quizData.HasImportBackup = true;
        NewEpoch(result);
        quizData.Revision++; SaveState();
        BroadcastState();
        BroadcastScores();
        BroadcastNotice(
            "Zaimportowano " + preparedRows.Count + " wpisów. " +
            (mode == "merge"
                ? "Ranking został połączony."
                : "Ranking został zastąpiony."));
    }

    private string NormalizeScoreName(string value)
    {
        value = (value ?? "").Trim().ToLowerInvariant();
        return Regex.Replace(value, @"\s+", " ");
    }

    private bool HasSpreadsheetFormulaPrefix(string value)
    {
        value = (value ?? "").TrimStart();
        if (value.Length == 0)
        {
            return false;
        }

        char first = value[0];
        return first == '=' || first == '+' || first == '-' || first == '@';
    }

    private string BuildNameScoreId(string userName)
    {
        return "name:" + NormalizeScoreName(userName);
    }

    private bool TryFindImportKey(Dictionary<string,ScoreEntry> scores,string name,bool explicitId,out string key) {
        var matches=scores.Where(x=>(!explicitId||x.Key.StartsWith("name:",StringComparison.OrdinalIgnoreCase))&&NormalizeScoreName(x.Value.UserName)==NormalizeScoreName(name)).ToList();
        Require(matches.Count<=1,"Niejednoznaczna stara nazwa w imporcie. Użyj Twitch User ID.");key=matches.Count==1?matches[0].Key:null;return key!=null;
    }
    private bool TryFindScoreKeyByName(
        Dictionary<string, ScoreEntry> scores,
        string userName,
        out string matchingKey)
    {
        matchingKey = null;
        string normalizedName = NormalizeScoreName(userName);
        if (String.IsNullOrWhiteSpace(normalizedName))
        {
            return false;
        }

        foreach (KeyValuePair<string, ScoreEntry> item in scores)
        {
            if (item.Key.StartsWith("name:", StringComparison.OrdinalIgnoreCase) && item.Value != null &&
                String.Equals(
                    NormalizeScoreName(item.Value.UserName),
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase))
            {
                matchingKey = item.Key;
                return true;
            }
        }
        return false;
    }

    public class CompetitiveSettings {
        public bool StreakEnabled {get;set;}
        public bool AchievementsEnabled {get;set;}
        public bool SeasonsEnabled {get;set;}
        [JsonProperty(ObjectCreationHandling=ObjectCreationHandling.Replace)] public List<StreakBonus> StreakBonuses {get;set;}=new List<StreakBonus>{new StreakBonus{Threshold=3},new StreakBonus{Threshold=5},new StreakBonus{Threshold=10}};
    }
    public class StreakBonus {public int Threshold {get;set;}public int Points {get;set;}}
    public class CompetitiveState {
        public int Schema {get;set;}=1;
        public string DataId {get;set;}=Guid.NewGuid().ToString("N");
        public CompetitiveSettings Settings {get;set;}=new CompetitiveSettings();
        public CompetitiveProjection Base {get;set;}=new CompetitiveProjection();
        public CompetitiveProjection Stats {get;set;}=new CompetitiveProjection();
        public List<Season> Seasons {get;set;}=new List<Season>();
        public string ActiveSeasonId {get;set;}="";
        public List<string> FrozenRounds {get;set;}=new List<string>();
    }
    public class CompetitiveProjection {
        public Dictionary<string,CompetitiveUser> Users {get;set;}=new Dictionary<string,CompetitiveUser>();
        public Dictionary<string,Dictionary<string,ScoreEntry>> SeasonScores {get;set;}=new Dictionary<string,Dictionary<string,ScoreEntry>>();
    }
    public class CompetitiveUser {
        public string UserId {get;set;}
        public string UserName {get;set;}
        public int CurrentStreak {get;set;}
        public int BestStreak {get;set;}
        public int StreakCorrectAnswers {get;set;}
        public int AchievementCorrectAnswers {get;set;}
        public int AchievementRounds {get;set;}
        public int AchievementStreak {get;set;}
        public Dictionary<string,long> Achievements {get;set;}=new Dictionary<string,long>();
    }
    public class AchievementDefinition {public string Id {get;set;}public string Metric {get;set;}public int Threshold {get;set;}public bool RequiresStreak {get;set;}}
    public class CompetitiveRound {public CompetitiveSettings Settings {get;set;}=new CompetitiveSettings();public string SeasonId {get;set;}="";}
    public class Season {
        public string Id {get;set;}=Guid.NewGuid().ToString("N");
        public string Name {get;set;}="";
        public string Kind {get;set;}="season";
        public string Status {get;set;}="created";
        public string CreateRequestId {get;set;}="";
        public long CreatedAt {get;set;}
        public long StartedAt {get;set;}
        public long EndedAt {get;set;}
        public Dictionary<string,ScoreEntry> Scores {get;set;}=new Dictionary<string,ScoreEntry>();
        public List<SeasonWinner> Winners {get;set;}=new List<SeasonWinner>();
    }
    public class SeasonWinner {public string UserId {get;set;}public string UserName {get;set;}public int Points {get;set;}}

    public class RoundSummary {
        public int Participants {get;set;}
        public int AcceptedVotes {get;set;}
        public int? SubmittedVotes {get;set;}
        public int? CorrectAnswers {get;set;}
        public double? CorrectPercent {get;set;}
        public long? TotalPoints {get;set;}
        public int? TopPoints {get;set;}
        public int? BiggestClimb {get;set;}
        public string Status {get;set;}="";
    }

    public class Root {
        public int Schema {get;set;}=2;
        public int Revision {get;set;}
        public int Epoch {get;set;}
        public string Migration {get;set;}="new schema 2";
        public bool BrowserMigrated {get;set;}
        public Dictionary<string,ScoreEntry> BaseScores {get;set;}=new Dictionary<string,ScoreEntry>();
        public Dictionary<string,ScoreEntry> Scores {get;set;}=new Dictionary<string,ScoreEntry>();
        public Dictionary<string,ScoreEntry> ImportBackup {get;set;}=new Dictionary<string,ScoreEntry>();
        public bool HasImportBackup {get;set;}
        public Dictionary<string,long> BaseTeamScores {get;set;}=new Dictionary<string,long>();
        public Dictionary<string,long> TeamScores {get;set;}=new Dictionary<string,long>();
        public Round Current {get;set;}
        public List<Round> History {get;set;}=new List<Round>();
        public List<Question> Bank {get;set;}=new List<Question>();
        public List<Question> Templates {get;set;}=new List<Question>();
        public Question Draft {get;set;}
        public List<QuizSet> Sets {get;set;}=new List<QuizSet>();
        public List<Team> Teams {get;set;}=new List<Team>();
        public Dictionary<string,string> Members {get;set;}=new Dictionary<string,string>();
        public Settings Settings {get;set;}=new Settings();
        public List<Question> Queue {get;set;}=new List<Question>();
        public string QueueSetId {get;set;}="";
        public int QueueIndex {get;set;}=-1;
        public int PreparedQueueIndex {get;set;}=-1;
        public CompetitiveState Competitive {get;set;}=new CompetitiveState();
        public bool Podium {get;set;}
        public bool OverlayVisible {get;set;}=true;
    }
    public class Question {
        public string Id {get;set;}=Guid.NewGuid().ToString("N");
        public string Name {get;set;}="";
        public string Text {get;set;}="";
        public string Category {get;set;}="";
        public List<string> Tags {get;set;}=new List<string>();
        public List<Option> Options {get;set;}=new List<Option>();
        public string Mode {get;set;}="QUIZ";
        public string Voting {get;set;}="LIVE";
        public string Policy {get;set;}="LAST";
        public int ChangeLimit {get;set;}=2;
        public int Duration {get;set;}=60;
        public bool Shuffle {get;set;}
        public bool AutoReveal {get;set;}
        public string Scoring {get;set;}="adjacent";
        public int FullPoints {get;set;}=3;
        public int PartialPoints {get;set;}=1;
        public bool Speed {get;set;}
        public int SpeedFast {get;set;}=2;
        public int SpeedSlow {get;set;}=1;
    }
    public class Option {
        public int AuthorIndex {get;set;}
        public string Id {get;set;}=Guid.NewGuid().ToString("N");
        public string Text {get;set;}="";
        public bool Correct {get;set;}
        public bool Partial {get;set;}
        public int? Points {get;set;}
    }
    public class Round {
        public CompetitiveRound Competitive {get;set;}
        public RoundSummary Summary {get;set;}
        public string Id {get;set;}=Guid.NewGuid().ToString("N");
        public Question Question {get;set;}=new Question();
        public string Status {get;set;}="open";
        public bool Visible {get;set;}=true;
        public Dictionary<string,Vote> Votes {get;set;}=new Dictionary<string,Vote>();
        public Dictionary<string,Award> Awards {get;set;}=new Dictionary<string,Award>();
        public long StartedAt {get;set;}
        public long EndedAt {get;set;}
        public long ClosesAt {get;set;}
        public long RemainingMs {get;set;}
        public long ElapsedMs {get;set;}
        public long ClockAt {get;set;}
        public bool Paused {get;set;}
        public bool Applied {get;set;}
        public bool Legacy {get;set;}
        public int Epoch {get;set;}
    }
    public class Vote {
        public string OptionId {get;set;}="";
        public string Name {get;set;}="";
        public long ElapsedMs {get;set;}
        public int Changes {get;set;}
        public string TeamId {get;set;}="";
        public bool Moderated {get;set;}
    }
    public class Award {
        public int Points {get;set;}
        public bool Correct {get;set;}
        public ScoreEntry Before {get;set;}
        public ScoreEntry After {get;set;}
    }
    public class ScoreEntry {
        public string UserId {get;set;}
        public string UserName {get;set;}
        public int Points {get;set;}
        public int CorrectAnswers {get;set;}
        public int AnsweredQuestions {get;set;}
        public long LastUpdatedAt {get;set;}
    }
    public class ImportedScoreEntry : ScoreEntry { }
    private class PreparedImportedScore {public ScoreEntry Entry {get;set;} public bool HasExplicitUserId {get;set;}}
    public class QuizSet {public string Id {get;set;}=Guid.NewGuid().ToString("N");public string Name {get;set;}="";public List<Question> Questions {get;set;}=new List<Question>();}
    public class Team {public string Id {get;set;}=Guid.NewGuid().ToString("N");public string Name {get;set;}="";}
    public class TeamEnvelope {public List<Team> Teams {get;set;} public Dictionary<string,string> Members {get;set;}}
    public class BrowserEnvelope {public List<Question> Templates {get;set;} public Question Draft {get;set;}}
    public class SnapshotEntry {public string Id {get;set;}public long At {get;set;}public string Reason {get;set;}public string Data {get;set;}}
    public class Backup {public string Format {get;set;}="LukiQuizBackup";public int Schema {get;set;}=1;public string AppVersion {get;set;}="2.2.0";public long CreatedAt {get;set;}public Root Data {get;set;}}
    public class BankEnvelope {public string Format {get;set;}="LukiQuizBank";public int Schema {get;set;}=1;public string AppVersion {get;set;}="2.2.0";public long CreatedAt {get;set;}public List<Question> Questions {get;set;}public List<QuizSet> Sets {get;set;}}
    public class Settings {
        public bool AnnounceRoundEventsInChat {get;set;}=false; // use panel.html to change this in options, default is off
        public string PointsCommand {get;set;}=CPHInline.PointsCommand;
        public string RankingCommand {get;set;}=CPHInline.RankingCommand;
        public string AdminCommand {get;set;}=CPHInline.AdminCommand;
        public string PanelLanguage {get;set;}="pl";
        public string OverlayLanguage {get;set;}="pl";
        public bool SyncLanguage {get;set;}=true;
        public string VoteParsing {get;set;}="STRICT";
        public ArchiveRetentionSettings ArchiveRetention {get;set;}=new ArchiveRetentionSettings();
        public bool TeamMode {get;set;}
        public bool Cooldowns {get;set;}=true;
        public int UserCooldown {get;set;}=10;
        public int GlobalCooldown {get;set;}=3;
        public bool Shortcuts {get;set;}
        public Dictionary<string,string> Keys {get;set;}=new Dictionary<string,string>{{"start","Ctrl+Alt+KeyS"},{"lock","Ctrl+Alt+KeyL"},{"reveal","Ctrl+Alt+KeyR"},{"cancel","Ctrl+Alt+KeyC"},{"next","Ctrl+Alt+ArrowRight"},{"previous","Ctrl+Alt+ArrowLeft"},{"pause","Ctrl+Alt+KeyP"},{"addtime","Ctrl+Alt+ArrowUp"},{"subtracttime","Ctrl+Alt+ArrowDown"},{"visibility","Ctrl+Alt+KeyH"},{"podium","Ctrl+Alt+KeyO"}};
        public OverlaySettings Overlay {get;set;}=new OverlaySettings();
    }
    public class ArchiveRetentionSettings {
        public bool Enabled {get;set;}
        public int MaxCount {get;set;}=100;
        public int MaxAgeDays {get;set;}=365;
        public bool IncludeManual {get;set;}
        public bool IncludeSafety {get;set;}
    }
    public class OverlaySettings {
        public string Layout {get;set;}="FULL";
        public string Accent {get;set;}="#9870ff";
        public int Scale {get;set;}=100;
        public int X {get;set;}=40;
        public int Y {get;set;}=40;
        public bool Leaderboard {get;set;}=true;
        public int Top {get;set;}=5;
        public bool Percentages {get;set;}=true;
        public bool Timer {get;set;}=true;
        public int Opacity {get;set;}=95;
        public bool Animations {get;set;}=true;
        public bool Sounds {get;set;}
        public int Volume {get;set;}=40;
        public bool Podium {get;set;}
    }
    public class LegacyOption {public string Text {get;set;}public int Count {get;set;}}
    public class LegacyState {
        public string Status {get;set;} public bool Visible {get;set;} public string Question {get;set;}
        public List<LegacyOption> Options {get;set;} public Dictionary<string,int> Votes {get;set;}
        public int CorrectIndex {get;set;} public int PointsPerCorrect {get;set;} public int PointsPerAdjacent {get;set;}
        public string ScoringMode {get;set;} public List<int> PartialIndices {get;set;} public bool PointsAwarded {get;set;}
        public long StartedAt {get;set;} public long ClosesAt {get;set;} public long UpdatedAt {get;set;}
        public string RoundId {get;set;} public int Revision {get;set;}
    }
}
