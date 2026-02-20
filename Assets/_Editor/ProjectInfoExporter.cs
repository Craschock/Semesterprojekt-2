using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System.Linq;
using System.Runtime.CompilerServices;

public class ProjectInfoExporter : Editor
{
    [MenuItem("Tools/Export Project Information")]
    public static void ExportProjectInfo()
    {
        StringBuilder sb = new StringBuilder();
        string projectPath = Path.GetDirectoryName(Application.dataPath); 
        
        sb.AppendLine("========================================");
        sb.AppendLine(" PROJECT INFORMATION EXPORT");
        sb.AppendLine("========================================");
        sb.AppendLine();

        // ---------------------------------------------------------
        // 1. SCENE HIERARCHY
        // ---------------------------------------------------------
        sb.AppendLine("### 1. SCENE HIERARCHY ###");
        var scene = SceneManager.GetActiveScene();
        sb.AppendLine($"Scene: {scene.name}");
        sb.AppendLine(new string('-', 30));
        
        foreach (GameObject obj in scene.GetRootGameObjects())
        {
            GetObjectHierarchy(obj, sb, 0);
        }
        sb.AppendLine();

        // ---------------------------------------------------------
        // 2. PROJECT STRUCTURE (Excluding .meta and .git)
        // ---------------------------------------------------------
        sb.AppendLine("### 2. PROJECT STRUCTURE ###");
        sb.AppendLine(new string('-', 30));
        
        string[] allFiles = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories);
        var filteredFiles = allFiles.Where(f => !f.EndsWith(".meta") && !f.Contains(".git")).ToArray();
        
        foreach (string file in filteredFiles)
        {
            string relativePath = file.Replace(projectPath, "").TrimStart('\\', '/').Replace("\\", "/");
            sb.AppendLine(relativePath);
        }
        sb.AppendLine();

        // ---------------------------------------------------------
        // 3. SCRIPT CONTENTS (All .cs files)
        // ---------------------------------------------------------
        sb.AppendLine("### 3. SCRIPT CONTENTS ###");
        var csFiles = allFiles.Where(f => f.EndsWith(".cs")).ToArray();
        
        foreach (string file in csFiles)
        {
            string relativePath = file.Replace(projectPath, "").TrimStart('\\', '/').Replace("\\", "/");
            sb.AppendLine("\n========================================");
            sb.AppendLine($"// FILE: {relativePath}");
            sb.AppendLine("========================================");
            sb.AppendLine(File.ReadAllText(file));
        }

        // ---------------------------------------------------------
        // EXPORT TO SCRIPT FOLDER
        // ---------------------------------------------------------
        string scriptFolder = GetCurrentScriptDirectory();
        string exportPath = Path.Combine(scriptFolder, "ProjectInformation.txt");
        File.WriteAllText(exportPath, sb.ToString(), Encoding.UTF8);
        
        Debug.Log($"✅ Project Information successfully exported to: {exportPath}");
        AssetDatabase.Refresh();
    }

    private static void GetObjectHierarchy(GameObject obj, StringBuilder sb, int indent)
    {
        string spaces = new string(' ', indent * 4);
        sb.AppendLine($"{spaces}● {obj.name}");

        Component[] components = obj.GetComponents<Component>();
        foreach (var comp in components)
        {
            if (comp == null) continue; 
            sb.AppendLine($"{spaces}    [{comp.GetType().Name}]");
        }

        foreach (Transform child in obj.transform)
        {
            GetObjectHierarchy(child.gameObject, sb, indent + 1);
        }
    }

    // Automatically retrieves the directory of this specific script file
    private static string GetCurrentScriptDirectory([CallerFilePath] string path = "")
    {
        return Path.GetDirectoryName(path);
    }
}