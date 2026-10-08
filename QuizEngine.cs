using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// LukiQuizEngine 2.0.0 GIGA UPDATE V2
public class CPHInline
{
    private const string AppVersion = "2.0.0";
    private const string BuildLabel = "FC1"; //oznaczenie identyfikatora, zmiana może wszystko zepsuć xdd
    private const string EnginePatch = "STYLE4"; // tak samo jak u góry
    private const int Protocol = 1;
    private const string PointsCommand = "!punkty"; // TO MOŻNA ZMIENIC, 
    private const string RankingCommand = "!ranking"; // TO TEŻ można
    private const string AdminCommand = "!quiz"; // NO I TO TEŻ można
    private const string DataVariable = "localQuiz_data_v2"; // NIE 
    private const string PublicVariable = "localQuiz_public_v2"; // NIE 
    private const string SnapshotVariable = "localQuiz_snapshots_v2"; // NIE 
    private const int MaxBytes = 8 * 1024 * 1024;
    private const int MaxImportedPayloadLength = 7500000;
    private const int MaxImportedScores = 25000;
    private const int MaxUserIdLength = 160;
    private const int MaxHistory = 100;
    private const int MaxSnapshotBytes = 32 * 1024 * 1024;
    private static readonly bool AnnounceRoundEventsInChat = false; // zmiana na true włącza wiadomosci na czacie twitch, dotycząca przebiegu rundy
    private static readonly object StateLock = String.Intern("LukiQuizEngine.v2.transaction");
    private static readonly Dictionary<Type,Dictionary<string,PropertyInfo>> propertyCache=new Dictionary<Type,Dictionary<string,PropertyInfo>>();
    private static readonly Regex VoteRegex = new Regex(@"^!(1[0-2]|[1-9])$", RegexOptions.CultureInvariant);
    private Timer roundTimer;
    private Root quizData;
    private string storedStateJson;
    private Dictionary<string,string> requestArgs = new Dictionary<string,string>();
    private string requestId = "";
    private string requestOperation = "";
    private bool legacyCountersRemoved;
    private bool requestFailed;
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

    public void Init() { EnsureTimer(); lock(StateLock) { try { if(!String.IsNullOrWhiteSpace(CPH.GetGlobalVar<string>(DataVariable,true))) {quizData=LoadState();if(Expire()){SaveState();BroadcastState();}} }catch(Exception ex){quizData=null;CPH.LogError("[LukiQuiz V2] Init: "+ex.GetType().Name);} } }
    public void Dispose() { lock (StateLock) { disposed = true; if (roundTimer != null) roundTimer.Dispose(); roundTimer = null; quizData = null; } }
    private void EnsureTimer() {
        lock (StateLock) { if (!disposed && roundTimer == null) roundTimer = new Timer(_ => ClockTick(), null, 250, 250); }
    }
    private void ClockTick() {
        lock (StateLock) {
            if (disposed || quizData == null) return;
            try {
                quizData = LoadState();
                if (Expire()) { SaveState(); BroadcastState(); }
                else if (stateBroadcastPending && Now() - lastBroadcastAt >= 100) BroadcastState();
            } catch (Exception ex) { quizData = null; CPH.LogError("[LukiQuiz V2] Timer stopped after error: " + ex.GetType().Name); }
        }
    }
    public bool Execute() {
        string wire = ReadHostArg("quizRequest");
        var captured = new Dictionary<string,string>();
        if (String.IsNullOrWhiteSpace(wire)) foreach (string key in ArgumentNames) captured[key] = ReadHostArg(key);
        EnsureTimer();
        lock (StateLock) {
            requestArgs = captured; requestId = ""; requestOperation = ""; requestFailed = false;
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
                quizData = LoadState();
                if (Expire()) SaveState();
                if (op != "") HandlePanelOperation(op); else HandleTwitchChat();
                if (requestId != "" && !requestFailed) {
                    Broadcast(new {type="quiz-result",requestId=requestId,build=BuildLabel,operation=op,ok=true,revision=quizData.Revision});
                    if (!IsReadOperation(op)) CPH.LogInfo("[LukiQuiz V2] COMMITTED build="+BuildLabel+" request="+requestId+" op="+op+" revision="+quizData.Revision);
                }
            } catch (Exception ex) {
                quizData = null;
                CPH.LogError("[LukiQuiz V2] FAILED build="+BuildLabel+" request="+requestId+" error="+ex.GetType().Name);
                BroadcastError("Operacja odrzucona: " + SafeError(ex));
            } finally { requestArgs = new Dictionary<string,string>(); requestId = ""; requestOperation = ""; }
        }
        return true;
    }
    private static bool IsReadOperation(string op) { return new[]{"sync","tick","getscores","getdata","diagnostics","exportbackup","exportbank","previewbackup","previewbank"}.Contains(op); }
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
        CheckTypes(token, typeof(T), 0, typeof(T)==typeof(PanelRequest)||typeof(T)==typeof(Root)||typeof(T)==typeof(Backup)||typeof(T)==typeof(BankEnvelope)||typeof(T)==typeof(Question)||typeof(T)==typeof(QuizSet)||typeof(T)==typeof(Settings));
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
            foreach(string required in props.Keys) Require(source.ContainsKey(required), "Niepełny schemat: "+required);
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
    }
    private void ValidateSettings(Settings s) {
        Require(s != null && s.Keys != null && s.Overlay != null,"Puste ustawienia.");
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
    private void SaveState() {
        Validate(quizData); string raw=JsonConvert.SerializeObject(quizData);
        Require(Encoding.UTF8.GetByteCount(raw)<=MaxBytes-65536,"Stan przekroczył 8 MiB. Zmniejsz bank lub historię. Operacja nie została zapisana.");
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
    private void EnsureRoundFinished() { Require(quizData.Current==null || quizData.Current.Status=="revealed" || quizData.Current.Status=="cancelled","Zakończ/anuluj aktywną rundę przed tą operacją."); }
    private void Confirm(string text) { Require(String.Equals((GetStringArg("confirmText")??"").Trim(),text,StringComparison.OrdinalIgnoreCase),"Wymagane potwierdzenie: "+text); }
    private void NewEpoch(Dictionary<string,ScoreEntry> scores) {
        quizData.Epoch++; quizData.BaseScores=Copy(scores); quizData.Scores=Copy(scores); quizData.BaseTeamScores=Copy(quizData.TeamScores);
        if(quizData.Current!=null&&(quizData.Current.Status=="open"||quizData.Current.Status=="locked")) quizData.Current.Epoch=quizData.Epoch;
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
        quizData.Podium=false;quizData.OverlayVisible=true; quizData.Current=new Round {Id=Id(),Question=q,StartedAt=Now(),ClockAt=Now(),ClosesAt=q.Duration>0?Now()+q.Duration*1000L:0,Epoch=quizData.Epoch};
        quizData.Revision++; SaveState(); BroadcastState(); if(AnnounceRoundEventsInChat)SendChat("Quiz: "+q.Text+" - głosuj !1-!"+q.Options.Count);
    }
    private void StartQuiz(string question,string answersJson,int durationSeconds,bool announce) {
        var texts=Read<List<string>>(answersJson);
        Start(new Question {Text=question,Duration=durationSeconds,FullPoints=1,PartialPoints=0,Options=texts.Select(t=>new Option {Text=t}).ToList()});
    }
    private void LockQuiz(bool expired,bool announce) {
        var r=quizData.Current; Require(r!=null,"Brak rundy.");
        if(expired) { if(Expire()) {SaveState();BroadcastState();} return; }
        Require(r.Status=="open"||r.Status=="locked","Brak otwartej rundy.");
        if(r.Status=="open") {
            long now=Now();
            r.ElapsedMs=Elapsed(r);
            if(r.Question.Duration>0) {
                r.RemainingMs=r.Paused?r.RemainingMs:(r.ClosesAt>0?Math.Max(0,r.ClosesAt-now):0);
            } else r.RemainingMs=0;
            r.ClockAt=now; r.ClosesAt=0; r.Paused=false; r.Status="locked";
        }
        quizData.Revision++; SaveState(); BroadcastState();if(announce&&AnnounceRoundEventsInChat)SendChat("Quiz: głosowanie zamknięte.");
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
        if(announce&&AnnounceRoundEventsInChat)SendChat("Quiz: głosowanie ponownie otwarte.");
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
        quizData.History.Add(Copy(r)); Replay(); Compact(); quizData.Current=Copy(quizData.History.Last());Compact();if(AnnounceRoundEventsInChat)SendChat(r.Question.Mode=="POLL"?"Ankieta zakończona.":"Quiz: wynik ujawniony, punkty przyznane.");
    }
    private void RevealAnswer(int correctIndex,int full,int partial,string mode,string partialJson,bool announce) {
        Require(quizData.Current!=null,"Brak rundy."); var q=Copy(quizData.Current.Question); ValidateRange(correctIndex,1,q.Options.Count,"Poprawna");
        q.FullPoints=full; q.PartialPoints=partial; q.Scoring=mode=="manual"?"manual":"adjacent";
        var indices=Read<List<int>>(partialJson); foreach(var o in q.Options) {o.Correct=false;o.Partial=false;o.Points=null;}
        q.Options[correctIndex-1].Correct=true; foreach(int n in indices) {ValidateRange(n,1,q.Options.Count,"Partial index");q.Options[n-1].Partial=true;}
        Reveal(quizData.Current,q); quizData.Revision++; SaveState(); BroadcastState(); BroadcastScores();
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
    private void ApplyRound(Round r,Dictionary<string,ScoreEntry> scores,Dictionary<string,long> teams) {
        r.Awards.Clear(); if(!r.Applied || r.Question.Mode!="QUIZ") return;
        foreach(var v in r.Votes.OrderBy(x=>x.Key,StringComparer.Ordinal)) {
            ScoreEntry e=EnsureScore(scores,v.Key,v.Value.Name); var before=Copy(e); var o=r.Question.Options.First(x=>x.Id==v.Value.OptionId);
            int points=Points(r.Question,o,v.Value); e.UserName=v.Value.Name; e.Points=checked(e.Points+points); e.AnsweredQuestions++;
            if(o.Correct)e.CorrectAnswers++;
            e.LastUpdatedAt=r.EndedAt; r.Awards[v.Key]=new Award {Points=points,Correct=o.Correct,Before=before,After=Copy(e)};
            if(!String.IsNullOrEmpty(v.Value.TeamId)) {if(!teams.ContainsKey(v.Value.TeamId))teams[v.Value.TeamId]=0;teams[v.Value.TeamId]+=points;}
        }
    }
    private void Replay() {
        var scores=Copy(quizData.BaseScores);var teams=Copy(quizData.BaseTeamScores);
        foreach(var r in quizData.History.Where(x=>x.Epoch==quizData.Epoch)) ApplyRound(r,scores,teams);
        quizData.Scores=scores;quizData.TeamScores=teams;
    }
    private void Compact() {
        while(quizData.History.Count>MaxHistory || (quizData.History.Count>1 && Encoding.UTF8.GetByteCount(JsonConvert.SerializeObject(quizData))>6*1024*1024)) {
            var r=quizData.History[0]; if(r.Epoch==quizData.Epoch) ApplyRound(r,quizData.BaseScores,quizData.BaseTeamScores);quizData.History.RemoveAt(0);
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
    private void CancelQuiz(bool announce) { if(quizData.Current!=null&&(quizData.Current.Status=="open"||quizData.Current.Status=="locked")) {quizData.Current.Status="cancelled";quizData.Current.EndedAt=Now();quizData.History.Add(Copy(quizData.Current));Compact();} quizData.Current=null;quizData.Podium=false;quizData.Revision++;SaveState();BroadcastState();BroadcastData(); }
    private void SetOverlayVisibility(bool show) {Require(quizData.Current!=null||quizData.Podium,"Brak rundy/podium.");quizData.OverlayVisible=show;if(quizData.Current!=null)quizData.Current.Visible=show;quizData.Revision++;SaveState();BroadcastState();}
    private void RegisterVote(int index) {
        var r=quizData.Current; if(r==null||r.Status!="open"||index<1||index>r.Question.Options.Count)return;
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
        var match=VoteRegex.Match(message);if(match.Success){RegisterVote(Int32.Parse(match.Groups[1].Value));return;}
        string id=GetStringArg("userId");if(id=="")id="name:"+GetStringArg("userName").ToLowerInvariant();
        if(message.Equals(PointsCommand,StringComparison.OrdinalIgnoreCase)&&AllowCommand("points",id)){SendUserPoints();return;}
        if(message.Equals(RankingCommand,StringComparison.OrdinalIgnoreCase)&&AllowCommand("ranking",id)){SendRanking();return;}
        if(message.StartsWith(AdminCommand,StringComparison.OrdinalIgnoreCase)&&(message.Length==AdminCommand.Length||Char.IsWhiteSpace(message[AdminCommand.Length]))&&IsChatAdmin())HandleAdminChatCommand(message);
    }
    private void SendUserPoints() {
        string id=GetStringArg("userId");if(id=="")id="name:"+GetStringArg("userName").ToLowerInvariant();var scores=quizData.Scores;ScoreEntry e;
        if(!scores.TryGetValue(id,out e)){string key;if(TryFindScoreKeyByName(scores,GetDisplayName(),out key))e=scores[key];}
        SendChat(GetDisplayName()+": "+(e==null?"0 pkt":e.Points+" pkt - poprawne "+e.CorrectAnswers+"/"+e.AnsweredQuestions));
    }
    private void SendRanking() {var top=SortScores(quizData.Scores).Take(5).Select((x,i)=>(i+1)+". "+SafeName(x.UserName)+" - "+x.Points+" pkt");SendChat("Quiz TOP 5: "+String.Join(" - ",top));}
    private void SendChat(string text) {text=(text??"").Replace("\r"," ").Replace("\n"," ");if(text.Length>470)text=text.Substring(0,469)+"…";CPH.SendMessage(text,true,true);}
    private string SafeName(string text){text=(text??"").Replace("\r"," ").Replace("\n"," ").Trim();return text==""?"Widz":text.Substring(0,Math.Min(80,text.Length));}
    private long UnixMilliseconds(){return Now();}
    private void HandlePanelOperation(string op) {
        if(op=="sync"||op=="tick"){BroadcastState();return;}
        if(op=="getscores"){BroadcastScores();return;}
        if(op=="getdata"){BroadcastData();return;}
        if(op=="diagnostics"){Broadcast(new{type="quiz-diagnostics",requestId=requestId,appVersion=AppVersion,build=BuildLabel,enginePatch=EnginePatch,protocol=Protocol,stateSchema=2,backupSchema=1,migration=quizData.Migration,aggregate="PASS",rankingCount=quizData.Scores.Count,historyCount=quizData.History.Count,snapshotCount=LoadSnapshots().Count,revision=quizData.Revision});return;}
        if(op=="start"){if(GetStringArg("payloadJson")!="")Start(Read<Question>(GetStringArg("payloadJson")));else StartQuiz(GetStringArg("question"),GetStringArg("answersJson"),GetIntArg("durationSeconds",0),true);return;}
        if(op=="lock"||op=="lockexpired"){LockQuiz(op=="lockexpired",true);return;}
        if(op=="reopen"){ReopenQuiz(true);return;}
        if(op=="reveal") {
            Require(quizData.Current!=null,"Brak rundy.");
            if(GetStringArg("payloadJson")!=""){Reveal(quizData.Current,Read<Question>(GetStringArg("payloadJson")));quizData.Revision++;SaveState();BroadcastState();BroadcastScores();}
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
        case "pause": case "resume": case "time": TimerOperation(op);Expire();break;
        case "moderatevote":ModerateVote();break;
        case "undo":Undo();break;
        case "rescore":Rescore();break;
        case "resetscores":
            Confirm("RESET"); EnsureRoundFinished(); Snapshot("reset ranking"); NewEpoch(new Dictionary<string,ScoreEntry>());
            quizData.TeamScores.Clear(); quizData.BaseTeamScores.Clear(); quizData.ImportBackup.Clear(); quizData.HasImportBackup=false; break;
        case "setscore": {
            string id=GetStringArg("userId"); Require(quizData.Scores.ContainsKey(id),"Nie znaleziono widza."); int p=GetIntArg("newPoints",-1); ValidateRange(p,0,100000000,"Korekta");
            Snapshot("manual score correction"); var scores=Copy(quizData.Scores); scores[id].Points=p; scores[id].LastUpdatedAt=Now(); NewEpoch(scores); break;
        }
        case "restoreimportbackup":EnsureRoundFinished();Confirm("COFNIJ");Require(quizData.HasImportBackup,"Brak kopii importu.");Snapshot("undo CSV import");NewEpoch(Copy(quizData.ImportBackup));quizData.ImportBackup.Clear();quizData.HasImportBackup=false;break;
        case "settings":var nextSettings=Read<Settings>(GetStringArg("payloadJson"));ValidateSettings(nextSettings);if(nextSettings.TeamMode!=quizData.Settings.TeamMode)EnsureRoundFinished();quizData.Settings=nextSettings;break;
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
            quizData.Queue=Copy(set.Questions);quizData.QueueSetId=set.Id;quizData.QueueIndex=-1;
            if(GetBoolArg("shuffle")){var random=new Random();quizData.Queue=quizData.Queue.OrderBy(x=>random.Next()).ToList();}quizData.Podium=false;break;
        }
        case "next":case "previous": {
            EnsureRoundFinished();Require(quizData.Queue!=null&&quizData.Queue.Count>0,"Najpierw wczytaj zestaw.");
            int n=quizData.QueueIndex+(op=="next"?1:-1);Require(n>=0,"Początek kolejki.");
            if(n>=quizData.Queue.Count){quizData.Podium=quizData.Settings.Overlay.Podium;quizData.OverlayVisible=true;quizData.QueueIndex=quizData.Queue.Count;break;}
            quizData.QueueIndex=n;Start(quizData.Queue[n]);return;
        }
        case "podium":EnsureRoundFinished();Require(quizData.Settings.Overlay.Podium,"Podium jest wyłączone w ustawieniach.");quizData.Podium=!quizData.Podium;if(quizData.Podium)quizData.OverlayVisible=true;break;
        case "teams":EnsureRoundFinished();var teams=Read<TeamEnvelope>(GetStringArg("payloadJson"));Require(teams!=null,"Puste drużyny.");quizData.Teams=teams.Teams;quizData.Members=teams.Members;break;
        case "restorebackup": {
            EnsureRoundFinished();Confirm("RESTORE");var b=Read<Backup>(GetStringArg("payloadJson"));ValidateBackup(b);Snapshot("full backup restore");
            int rev=quizData.Revision;quizData=Copy(b.Data);ConsolidateLegacyTemplates(quizData);quizData.Revision=Math.Max(rev,quizData.Revision);SanitizeRestoredRound();break;
        }
        case "restoresnapshot": {
            EnsureRoundFinished();Confirm("RESTORE");var s=LoadSnapshots().FirstOrDefault(x=>x.Id==GetStringArg("itemId"));Require(s!=null,"Snapshot nie istnieje.");
            var restored=Read<Root>(s.Data);ValidateBackup(new Backup{Data=restored,CreatedAt=s.At});Snapshot("before snapshot restore");int rev=quizData.Revision;quizData=restored;ConsolidateLegacyTemplates(quizData);quizData.Revision=Math.Max(rev,quizData.Revision);SanitizeRestoredRound();break;
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
        if(op=="resetscores") { CPH.LogInfo("[LukiQuiz V2] RESET rankingu zapisany, revision="+quizData.Revision+", scores="+quizData.Scores.Count);  }
        else if(op=="setscore") { CPH.LogInfo("[LukiQuiz V2] Korekta punktów zapisana, revision="+quizData.Revision);  }
        else if(op=="savequestion"||op=="savetemplate")BroadcastNotice("Szablon zapisany.");
        else if(op=="importbank")BroadcastNotice("Szablony zostały zaimportowane.");
    }
    private void ValidateBackup(Backup b) {Require(b!=null&&b.Format=="LukiQuizBackup"&&b.Schema==1&&b.Data!=null,"Nieobsługiwany backup.");ValidateText(b.AppVersion,1,30,"AppVersion");ValidateRange(b.CreatedAt,0,4102444800000L,"Backup timestamp");Validate(b.Data);var copy=Copy(b.Data);var previous=quizData;try{quizData=copy;Replay();Require(JsonConvert.SerializeObject(copy.Scores)==JsonConvert.SerializeObject(b.Data.Scores)&&JsonConvert.SerializeObject(copy.TeamScores)==JsonConvert.SerializeObject(b.Data.TeamScores),"Ranking backupu nie zgadza się z historią.");}finally{quizData=previous;}}
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
            visible=quizData.OverlayVisible&&(quizData.Podium||(r!=null&&r.Visible)),question=r==null?"":r.Question.Text,roundId=r==null?"":r.Id,mode=r==null?"QUIZ":r.Question.Mode,
            voting=r==null?"LIVE":r.Question.Voting,totalVotes=counts&&r!=null?r.Votes.Count:0,distribution=distribution,countsVisible=counts,
            options=options.Select((o,i)=>new{index=i+1,id=o.Id,text=o.Text,count=distribution?r.Votes.Count(v=>v.Value.OptionId==o.Id):0,correct=reveal&&o.Correct,partial=reveal&&!o.Correct&&o.Partial,points=reveal&&r.Question.Mode=="QUIZ"?(o.Points.HasValue?o.Points.Value:o.Correct?r.Question.FullPoints:o.Partial?r.Question.PartialPoints:0):0}).ToList(),
            closesAt=r!=null?r.ClosesAt:0,paused=r!=null&&r.Paused,remainingMs=r!=null?r.RemainingMs:0,
            leaderboard=SortScores(quizData.Scores).Take(10).Select((e,i)=>new{position=i+1,userId=e.UserId,userName=SafeName(e.UserName),points=e.Points,correct=e.CorrectAnswers,answered=e.AnsweredQuestions}).ToList(),
            overlay=quizData.Settings.Overlay,podium=quizData.Podium&&quizData.Settings.Overlay.Podium,
            teamMode=quizData.Settings.TeamMode,teams=quizData.Teams.Select(t=>new{id=t.Id,name=t.Name,points=quizData.TeamScores.ContainsKey(t.Id)?quizData.TeamScores[t.Id]:0}).OrderByDescending(t=>t.points).ToList()
        };
    }
    private void BroadcastState() {
        lastBroadcastAt=Now();stateBroadcastPending=false;var p=PublicState();CPH.SetGlobalVar(PublicVariable,JsonConvert.SerializeObject(p),false);Broadcast(p);
        Broadcast(new{type="quiz-admin",appVersion=AppVersion,revision=quizData.Revision,current=quizData.Current,epoch=quizData.Epoch,queueIndex=quizData.QueueIndex,queueCount=quizData.Queue==null?0:quizData.Queue.Count});
    }
    private object ScorePayload() { return SortScores(quizData.Scores).Select((e,i)=>new{position=i+1,userId=e.UserId,userName=SafeName(e.UserName),points=e.Points,correct=e.CorrectAnswers,answered=e.AnsweredQuestions,lastUpdatedAt=e.LastUpdatedAt}).ToList(); }
    private void BroadcastScores() {Broadcast(new{type="quiz-scores",appVersion=AppVersion,revision=quizData.Revision,scores=ScorePayload()});}
    private void BroadcastData() {
        object snapshotInfo;
        try { snapshotInfo=LoadSnapshots().Select(s=>new{id=s.Id,at=s.At,reason=s.Reason,bytes=Encoding.UTF8.GetByteCount(s.Data)}).ToList(); }
        catch(Exception ex) { CPH.LogWarn("[LukiQuiz V2] Lista snapshotów niedostępna dla panelu: "+ex.GetType().Name); snapshotInfo=new object[0]; }
        Broadcast(new{type="quiz-data",appVersion=AppVersion,revision=quizData.Revision,data=quizData,snapshots=snapshotInfo});
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
        string rest = message.Length > AdminCommand.Length
            ? message.Substring(AdminCommand.Length).Trim()
            : "";
        if (String.IsNullOrWhiteSpace(rest) ||
            String.Equals(rest, "pomoc", StringComparison.OrdinalIgnoreCase) ||
            String.Equals(rest, "help", StringComparison.OrdinalIgnoreCase))
        {
            SendChat(
                "Quiz admin: " + AdminCommand +
                " start Pytanie | Odp. 1 | Odp. 2 ... - " + AdminCommand +
                " zamknij - " + AdminCommand +
                " wynik NUMER [PEŁNE PUNKTY] [PUNKTY ZA ODPOWIEDŹ OBOK] - " +
                AdminCommand + " anuluj - " + AdminCommand + " pokaz/ukryj");
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
                    "Quiz: użycie: " + AdminCommand +
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

        SendChat("Quiz: nieznana komenda. Wpisz " + AdminCommand + " pomoc");
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
    public class Backup {public string Format {get;set;}="LukiQuizBackup";public int Schema {get;set;}=1;public string AppVersion {get;set;}="2.0.0";public long CreatedAt {get;set;}public Root Data {get;set;}}
    public class BankEnvelope {public string Format {get;set;}="LukiQuizBank";public int Schema {get;set;}=1;public string AppVersion {get;set;}="2.0.0";public long CreatedAt {get;set;}public List<Question> Questions {get;set;}public List<QuizSet> Sets {get;set;}}
    public class Settings {
        public bool TeamMode {get;set;}
        public bool Cooldowns {get;set;}=true;
        public int UserCooldown {get;set;}=10;
        public int GlobalCooldown {get;set;}=3;
        public bool Shortcuts {get;set;}
        public Dictionary<string,string> Keys {get;set;}=new Dictionary<string,string>{{"start","Ctrl+Alt+KeyS"},{"lock","Ctrl+Alt+KeyL"},{"reveal","Ctrl+Alt+KeyR"},{"cancel","Ctrl+Alt+KeyC"},{"next","Ctrl+Alt+ArrowRight"},{"previous","Ctrl+Alt+ArrowLeft"},{"pause","Ctrl+Alt+KeyP"},{"addtime","Ctrl+Alt+ArrowUp"},{"subtracttime","Ctrl+Alt+ArrowDown"},{"visibility","Ctrl+Alt+KeyH"},{"podium","Ctrl+Alt+KeyO"}};
        public OverlaySettings Overlay {get;set;}=new OverlaySettings();
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
