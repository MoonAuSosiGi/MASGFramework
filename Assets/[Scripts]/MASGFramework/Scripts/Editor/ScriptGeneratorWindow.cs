using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class ScriptGeneratorWindow : EditorWindow
{
    /// <summary>
    /// 실제 코드 템플릿이 들어갈 공간
    /// </summary>
    private Dictionary<string, JSONObject> codeTemplateDic = new Dictionary<string, JSONObject>();

    /// <summary>
    /// 옵션 코드블록 딕셔너리. 그룹화 되어있지 않음 
    /// </summary>
    private Dictionary<string, JSONObject> optionCodeBlockDic = new Dictionary<string, JSONObject>();

    /// <summary>
    /// 옵션 코드 블록. 그룹별로 처리함
    /// </summary>
    private Dictionary<string, Dictionary<string, JSONObject>> optionCodeGroupDic = new Dictionary<string, Dictionary<string, JSONObject>>();

    /// <summary>
    /// 그룹별로 처리되는 옵션의 bool 변수 저장 딕셔너리
    /// </summary>
    private Dictionary<string, bool> optionCodeGroupCheckBoxDic = new Dictionary<string, bool>();
    
    /// <summary>
    /// 코드 템플릿에 대응되는 룰이 들어갈 공간 key는 코드 템플릿의 이름
    /// </summary>
    private Dictionary<string, JSONObject> ruleDic = new Dictionary<string, JSONObject>();

    /// <summary>
    /// 옵션 템플릿들의 상태를 저장할 공간 (그룹화 되어있지 않은 것들)
    /// </summary>
    private List<bool> optionTemplateStateList = new List<bool>();

    /// <summary>
    /// 현재 선택중인 클래스 이름
    /// </summary>
    private string currentClassName = "";
    /// <summary>
    /// 현재 클래스의 템플릿 리스트 중 선택한 템플릿
    /// </summary>
    private string currentCodeTemplate = "";
    /// <summary>
    /// 현재 선택된 코드 포맷의 json
    /// </summary>
    private JSONObject currentSelectJson = null;
    /// <summary>
    /// 기본적으로 생성되는 위치. 비어있을 경우 선택해야한다.
    /// </summary>
    private string defaultPath = "";
    /// <summary>
    /// 클래스 이름을 입력 받을 라벨 변수 
    /// </summary>
    private string classNameLabel = "";
    /// <summary>
    /// 네임스페이스를 입력받을 라벨 변수 
    /// </summary>
    private string classNameSpaceLabel = "";
    /// <summary>
    /// 현재 선택한 출력 경로 
    /// </summary>
    private string selectOutputPath = "";
    
    /// <summary>
    /// 변경을 감지할 워쳐 클래스
    /// </summary>
    private FileSystemWatcher watcher;

    [MenuItem("Tools/Script Generator")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ScriptGeneratorWindow));
    }

    private void OnDestroy()
    {
        OnClearData(true);
    }

    void OnClearData(bool isWatcherClear)
    {
        codeTemplateDic.Clear();
        optionTemplateStateList.Clear();
        optionCodeGroupCheckBoxDic.Clear();
        ruleDic.Clear();
        if (isWatcherClear)
        {
            watcher?.Dispose();
        }
    }

    private void OnFocus()
    {
        if (codeTemplateDic.Count == 0)
        {
            RegisterFile();
            LoadProcess();
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical();

        // >> 1. 클래스 템플릿 선택 가능한 드롭다운 표시 
        DrawClassTemplateSelector();

        // >> 2. 클래스의 템플릿 리스트 표시
        DrawClassTemplate();
        
        // >> 3. 옵션 템플릿 표시 (체크 박스 형태)
        DrawClassOptionTemplate();
        
        // >> 4. 클래스 네임스페이스, 이름 등 관련 필드 표시
        DrawClassField();
        
        // >> 5. 생성하기 버튼 표시
        DrawCreateButton();

        EditorGUILayout.EndVertical();
    }

    void DrawClassTemplateSelector()
    {
        EditorGUILayout.LabelField("생성할 클래스 선택");
        EditorGUILayout.BeginHorizontal();
        {
            if (EditorGUILayout.DropdownButton(new GUIContent(currentClassName), FocusType.Keyboard))
            {
                List<string> templateList = new List<string>();

                foreach (var template in codeTemplateDic)
                {
                    templateList.Add(template.Key);
                }
            
                GenericMenu _menu = new GenericMenu();
                foreach (var item in templateList)
                {
                    if (string.IsNullOrEmpty(item))
                    {
                        continue;
                    }
                    _menu.AddItem(new GUIContent(item), currentClassName.Equals(item), OnClassSelected, item);
                }
                _menu.ShowAsContext();
            }

            if (GUILayout.Button("데이터 파일 위치 열기"))
            {
                string rootPath = Path.Combine( Application.dataPath, "../[CodeTemplates]/" );
                Process.Start(rootPath);
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    /// <summary>
    /// 실제 해당 클래스 템플릿의 json을 읽어와서 서브 템플릿 드랍박스를 보여준다.
    /// </summary>
    void DrawClassTemplate()
    {
        if (codeTemplateDic.Count == 0 || 
            string.IsNullOrEmpty(currentClassName) || 
            codeTemplateDic.ContainsKey(currentClassName) == false)
            return;
        JSONObject json = codeTemplateDic[currentClassName];
        
        EditorGUILayout.LabelField($"{currentClassName} 코드 템플릿 ");
        if (EditorGUILayout.DropdownButton(new GUIContent(currentCodeTemplate), FocusType.Keyboard))
        {
            string templateKey = "templates";
            if (json.IsExists(templateKey) == false)
                return;
            var templateList = json[templateKey].list;
            List<string> subTemplateList = new List<string>();
            
            foreach (var template in templateList)
            {
                subTemplateList.Add(template["name"].safeStr);    
            }
            
            GenericMenu _menu = new GenericMenu();
            foreach (var item in subTemplateList)
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }
                _menu.AddItem(new GUIContent(item), currentCodeTemplate.Equals(item), OnClassTemplateSelected, item);
            }
            _menu.ShowAsContext();
        }
        EditorGUILayout.Space();
    }
    private void DrawClassOptionTemplate()
    {
        if (codeTemplateDic.Count == 0 || 
            string.IsNullOrEmpty(currentClassName) || 
            codeTemplateDic.ContainsKey(currentClassName) == false ||
            optionCodeBlockDic.Count == 0 && optionCodeGroupDic.Count == 0)
            return;
        
        EditorGUILayout.LabelField($"{currentClassName}의 옵션 템플릿");

        // >> group
        var optionGroupKeys = optionCodeGroupDic.Keys.ToList();
        for (int i = 0; i < optionGroupKeys.Count; i++)
        {
            string groupKey = optionGroupKeys[i];

            foreach (var optionData in optionCodeGroupDic[groupKey])
            {
                if (optionCodeGroupCheckBoxDic.ContainsKey(groupKey) == false)
                    optionCodeGroupCheckBoxDic.Add(groupKey, false);

                optionCodeGroupCheckBoxDic[groupKey] = EditorGUILayout.ToggleLeft(
                    $"{optionData.Key} ::{optionData.Value["name"].safeStr}", optionCodeGroupCheckBoxDic[groupKey]);
            }
        }

        // >>  no group
        var optionKeys = optionCodeBlockDic.Keys.ToList();

        for (int i = 0; i < optionKeys.Count; i++)
        {
            string key = optionKeys[i];

            if(i >= optionTemplateStateList.Count)
                optionTemplateStateList.Add(false);
            var optionData = optionCodeBlockDic[key];

            optionTemplateStateList[i] =
                EditorGUILayout.ToggleLeft($"{optionData["optionName"].safeStr} :: {optionData["name"].safeStr}",
                    optionTemplateStateList[i]);
        }
    }

    void DrawClassField()
    {
        if (currentSelectJson == null)
        {
            return;
        }
        classNameLabel = EditorGUILayout.TextField("클래스 이름", classNameLabel);
        
        if (currentSelectJson.IsExists("hasNameSpace") && currentSelectJson["hasNameSpace"].safeBool)
        {
            classNameSpaceLabel = EditorGUILayout.TextField("클래스 네임스페이스 지정", classNameSpaceLabel);    
        }

        // 기본 경로가 비어있다면, 선택할 수 있게 한다.
        if (string.IsNullOrEmpty(defaultPath))
        {
            selectOutputPath = GetSelectedPathOrFallback();
            EditorGUILayout.LabelField($"생성할 코드 경로 {selectOutputPath}");
        }
        // 그게 아니라면, 선택 불가
        else
        {
            EditorGUILayout.LabelField($"생성할 코드 경로 {defaultPath}");
        }
    }

    void DrawCreateButton()
    {
        if (currentSelectJson == null)
        {
            return;
        }
        
        if (GUILayout.Button("코드 생성"))
        {
            CreateClassFile();
        }
    }

    string GetCurrentFirstClassName()
    {
        if (codeTemplateDic.Count == 0)
            return null;
        return codeTemplateDic.Keys.First();
    }
    string GetCurrentFirstTemplateName()
    {
        if ( string.IsNullOrEmpty(currentClassName) || codeTemplateDic.ContainsKey(currentClassName) == false)
            return null;
        
        string templateKey = "templates";
        JSONObject json = codeTemplateDic[currentClassName];
        if (json.IsExists(templateKey) == false)
            return null;
        var templateList = json[templateKey].list;
        if (templateList.Count > 0)
            return templateList[0]["name"].safeStr;
        return null;
    }
    
    void OnClassSelected(object value)
    {
        if (value == null)
            return;
        
        currentClassName = value.ToString();
        RefreshOptionTemplateData();
        
        OnClassTemplateSelected(GetCurrentFirstTemplateName());
    }

    void OnClassTemplateSelected(object value)
    {
        if (value == null)
            return;
        currentCodeTemplate = value.ToString();

        if (codeTemplateDic.ContainsKey(currentClassName))
        {
            var currentClass = codeTemplateDic[currentClassName];

            var templateList = currentClass["templates"].list;

            foreach (var template in templateList)
            {
                if (template["name"].safeStr == currentCodeTemplate)
                {
                    currentSelectJson = template;
                    break;
                }
            }
            
            defaultPath = currentClass["defaultPath"].safeStr;
        }
    }

    #region File IO

    async void LoadProcess()
    {
        currentClassName = "";
        codeTemplateDic.Clear();
        
        string rootPath = Path.Combine( Application.dataPath, "../[CodeTemplates]/" );

        string[] templateFiles = Directory.GetFiles(rootPath);

        foreach (var templateFile in templateFiles)
        {
            if (string.IsNullOrEmpty(templateFile) || templateFile.Contains(".json") == false)
            {
                continue;
            }
            await LoadTemplateFile(templateFile);
        }
        
        OnClassSelected(GetCurrentFirstClassName());
    }

    async Task LoadTemplateFile(string templateFilePath)
    {
        if (string.IsNullOrEmpty(templateFilePath) || templateFilePath.Contains(".json") == false)
            return;
        
        Action action = delegate
        {
            using (StreamReader sr = new StreamReader(new FileStream(templateFilePath, FileMode.Open)))
            {
                StringBuilder jsonBuilder = new StringBuilder();

                try
                {
                    jsonBuilder.Append(sr.ReadToEnd());
                    AddJsonData(jsonBuilder.ToString());
                    Debug.LogWarning(jsonBuilder.ToString());
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"LoadTemplateFile Failed {templateFilePath} error -> {e}");
                }
            }    
        };
        await Task.Factory.StartNew(action);
    }

    void AddJsonData(string jsonStrData)
    {
        if (string.IsNullOrEmpty(jsonStrData))
        {
            Debug.LogError("데이터가 null입니다.");
            return;
        }

        // >> 원하는 형태의 데이터인지 검증 시작
        JSONObject json = new JSONObject(jsonStrData);

        // >> 1. 필수 키 체크 
        string className = "className";
        string namePrefix = "namePrefix";
        string template = "templates";
        string optionTemplate = "optionTemplate";
        string rule = "rule";

        string[] keys =
        {
            className, 
            namePrefix, 
            template, 
            optionTemplate,
            rule
        };
        foreach (var key in keys)
        {
            if (json.IsExists(key) == false)
            {
                Debug.LogError($"필수 키가 존재하지 않습니다. 없는 키 : {key}");
                return;
            }
        }
        
        // >> 2. 데이터 등록
        codeTemplateDic.Add(json[className].safeStr, json);
        var templateList = json[template].list;
        var ruleList = json[rule].list;

        if (templateList.Count != ruleList.Count)
        {
            Debug.LogError($"템플릿과 룰의 갯수가 맞지 않습니다. {className}");
            return;
        }
        
        for(int i = 0; i < templateList.Count; i++)
        {
            var ruleData = ruleList[i];
            var templateData = templateList[i];
            ruleDic.Add(templateData["name"].safeStr, ruleData);
        }
    }

    void RefreshOptionTemplateData()
    {
        if (codeTemplateDic.Count == 0 || codeTemplateDic.ContainsKey(currentClassName) == false)
            return;
        
        optionCodeBlockDic.Clear();
        optionTemplateStateList.Clear();
        optionCodeBlockDic.Clear();
        optionCodeGroupDic.Clear();
        optionCodeGroupCheckBoxDic.Clear();
        ruleDic.Clear();
        
        var json = codeTemplateDic[currentClassName];
        
        var optionList = json["optionTemplate"].list;
        foreach (var option in optionList)
        {
            if (option.IsExists("group"))
            {
                string groupKey = option["group"].safeStr;
                if (optionCodeGroupDic.ContainsKey(groupKey) == false)
                {
                    Dictionary<string, JSONObject> dic = new Dictionary<string, JSONObject>();
                    dic.Add(option["name"].safeStr, option);
                    optionCodeGroupDic.Add(groupKey, dic);
                }
                else
                {
                    optionCodeGroupDic[groupKey].Add(option["name"].safeStr, option);
                }
            }
            else
            {
                // 그룹이 없는 경우엔 기본으로
                optionCodeBlockDic.Add(option["name"].safeStr, option);
            }
            
        }

        var templateList = json["templates"].list;
        var ruleList = json["rule"].list;

        if (templateList.Count != ruleList.Count)
        {
            Debug.LogError($"템플릿과 룰의 갯수가 맞지 않습니다. {currentClassName}");
            return;
        }
        
        for(int i = 0; i < templateList.Count; i++)
        {
            var ruleData = ruleList[i];
            var templateData = templateList[i];
            ruleDic.Add(templateData["name"].safeStr, ruleData);
        }
    }
    
    void CreateClassFile()
    {
        string outputPath = (string.IsNullOrEmpty(defaultPath)) ? selectOutputPath : defaultPath;
        
        if (currentSelectJson == null ||
            string.IsNullOrEmpty(outputPath) ||
            string.IsNullOrEmpty(classNameLabel))
        {
            return;
        }

        var data = codeTemplateDic[currentClassName];
        string namePrefix = data["namePrefix"].safeStr;
        string result = GetCodeString();

        string resultPath = $"{outputPath}/{namePrefix}{classNameLabel}.cs";
        // >> 디렉토리 생성후 처리 
        if (CheckDirectoryAndCreate(outputPath))
        {
            Action createFile = delegate { File.WriteAllText(resultPath, result); };
        
            if (File.Exists(resultPath))
            {
                if (EditorUtility.DisplayDialog("주의", $"{resultPath} 이미 존재하는 파일입니다. 덮어씌위시겠습니까?", "예", "아니요"))
                {
                    createFile();
                }
            }
            else
            {
                createFile();
            }
        }
    }

    /// <summary>
    /// 디렉토리를 체크하고, 없을 경우 생성한다.
    /// </summary>
    /// <returns>true 리턴시 디렉토리가 있다는 뜻</returns>
    bool CheckDirectoryAndCreate(string directoryPath)
    {
        if (Directory.Exists(directoryPath) == false)
        {
            if (EditorUtility.DisplayDialog("주의", $"{directoryPath} 존재하지 않는 경로입니다. 생성하시겠습니까?", "예", "아니요"))
            {
                Directory.CreateDirectory(directoryPath);
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    void RegisterFile()
    {
        if (watcher != null)
            return;
        
        string rootPath = Path.Combine( Application.dataPath, "../[CodeTemplates]/" );
        
        watcher = new FileSystemWatcher(rootPath, "*.json");

        watcher.NotifyFilter = NotifyFilters.CreationTime |
                                NotifyFilters.Attributes |
                                NotifyFilters.DirectoryName |
                                NotifyFilters.FileName |
                                NotifyFilters.LastAccess |
                                NotifyFilters.LastWrite |
                                NotifyFilters.Security |
                                NotifyFilters.Size;

        watcher.IncludeSubdirectories = true;

        watcher.Changed += OnChanged;
        watcher.Created += OnCreated;
        watcher.Deleted += OnDeleted;
        

        watcher.EnableRaisingEvents = true;
    }
    
    private void OnChanged(object source, FileSystemEventArgs e)
    {
        OnClearData(false);
        LoadProcess();
    }

    private void OnCreated(object source, FileSystemEventArgs e)
    {
        OnClearData(false);
        LoadProcess();
    }

    private void OnDeleted(object source, FileSystemEventArgs e)
    {
        OnClearData(false);
        LoadProcess();
    }
    #endregion

    string GetCodeString()
    {
        if (string.IsNullOrEmpty(currentClassName) ||
            codeTemplateDic.ContainsKey(currentClassName) == false ||
            ruleDic.ContainsKey(currentCodeTemplate) == false)
            return "";
        
        var data = codeTemplateDic[currentClassName];
        string namePrefix = data["namePrefix"].safeStr;

        // rule에 따른 처리
        var ruleData = ruleDic[currentCodeTemplate];

        List<string> tokenList = new List<string>();
        int index = 0;
        while (ruleData.IsExists("{" + index + "}"))
        {
            string token = ruleData["{" + index + "}"].safeStr;
            string space = GetNeedTokenSpace(index);
            
            if (token.Contains("option_"))
            {
                // 여러개가 들어 가는 경우 
                if (token.Contains(","))
                {
                    string[] tokenArr = token.Split(',');

                    StringBuilder tokenBuilder = new StringBuilder();
                    
                    for(int i = 0; i < tokenArr.Length; i++)
                    {
                        string msg = GetOptionTokenCode(tokenArr[i]);
                        if (string.IsNullOrEmpty(msg) == false)
                        {
                            tokenBuilder.Append(msg);
                            if(i != tokenArr.Length-1)
                                tokenBuilder.Append($"\n{space}");    
                        }
                        
                    }
                    tokenList.Add(tokenBuilder.ToString());
                }
                else
                {
                    tokenList.Add(GetOptionTokenCode(token));    
                }
                
            }
            else
            {
                bool isNameSpace = currentSelectJson.IsExists("hasNameSpace") &&
                                   currentSelectJson["hasNameSpace"].safeBool &&
                                   string.IsNullOrEmpty(classNameSpaceLabel) == false; 

                switch (token)
                {
                    case "fullName": token = $"{namePrefix}{classNameLabel}";
                        break;
                    case "name": token = classNameLabel;
                        break;
                    case "name_quotes": token = $"\"{classNameLabel}\"";
                        break;
                    case "namespace_open":
                        if (isNameSpace)
                            token = string.Format("namespace {0}\n{{", classNameSpaceLabel);
                        else
                            token = "";
                        break;
                    case "namespace_close":
                        if (isNameSpace)
                            token = "}";
                        else
                            token = "";
                        
                        break;
                }
                
                tokenList.Add(token);
            }
            index++;
        }
        
        // 나머지는 빈칸 채우기 
        index = 0;
        string currentCodeFormatStr = currentSelectJson["code"].safeStr;

        if (string.IsNullOrEmpty(currentCodeFormatStr))
        {
            Debug.LogError($"code block이 비어있습니다.");
            return "";
        }

        while (currentCodeFormatStr.Contains("{" + index + "}"))
        {
            index++;
        }

        if (index >= tokenList.Count)
        {
            int createCount = index - tokenList.Count;
            for (int i = 0; i < createCount; i++)
            {
                tokenList.Add("");
            }
        }

        string result = "";
        try
        {
            result = string.Format(currentCodeFormatStr, tokenList.ToArray());
        }
        catch (Exception)
        {
            Debug.LogError($"code block이 잘못되었습니다.");
        }
        
        return result;
    }

    string GetNeedTokenSpace(int index)
    {
        string formatStr = currentSelectJson["code"].safeStr;
        string[] formatStrArr = formatStr.Split('\n');

        foreach (var token in formatStrArr)
        {
            if (token.Contains("{" + index + "}"))
            {
                return token.Substring(0, token.IndexOf('{'));
            }
        }

        return "";
    }

    string GetOptionTokenCode(string optionToken)
    {
        if (optionToken.Contains("option_") == false ||
            optionToken.Contains(","))
        {
            return "";
        }
        // 옵션 데이터
        string optionName = optionToken.Substring("option_".Length);

        return GetOptionCode(optionName);    
    }

    string GetOptionCode(string optionName)
    {
        var optionKeys = optionCodeBlockDic.Keys.ToList();
        if (optionKeys.Count != optionTemplateStateList.Count)
        {
            Debug.LogError("옵션 상태와, 옵션 리스트의 갯수가 다릅니다.");
            return "";
        }

        foreach (var optionGroup in optionCodeGroupCheckBoxDic)
        {
            if (optionGroup.Value)
            {
                var dic = optionCodeGroupDic[optionGroup.Key];

                foreach (var optionData in dic)
                {
                    if (optionData.Value["optionName"].safeStr == optionName)
                    {
                        return optionData.Value["code"].safeStr;
                    }
                }
            }
        }

        for(int i = 0; i < optionKeys.Count; i++)
        {
            if (optionTemplateStateList[i])
            {
                var optionData = optionCodeBlockDic[optionKeys[i]];
                if (optionName == optionData["optionName"].safeStr)
                {
                    return optionData["code"].safeStr;
                }
            }
        }

        return "";
    }

    public static string GetSelectedPathOrFallback()
    {
        string path = "Assets";
		
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if ( !string.IsNullOrEmpty(path) && File.Exists(path) ) 
            {
                path = Path.GetDirectoryName(path);
                break;
            }
        }
        return path;
    }
}