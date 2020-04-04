using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace acsetup
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!File.Exists(AtCoderCliSettingFileName))
            {
                throw new FileNotFoundException(string.Format("{0} was not found", AtCoderCliSettingFileName));
            }

            dynamic jsonData = Parse(File.ReadAllText(AtCoderCliSettingFileName));
            CreateTestFile(jsonData);
            CopyTemplate(jsonData);
        }

        /// <summary>
        /// atcoder-cli の設定ファイル名
        /// </summary>
        static string AtCoderCliSettingFileName = "contest.acc.json";

        /// <summary>
        /// VisualStudio用テストファイルの書き出し先ディレクトリ。
        /// とりあえずカレントディレクトリに決め打ち。
        /// 設定ファイルを読むように変える？
        /// </summary>
        static string TestFileDir = Environment.CurrentDirectory;

        /// <summary>
        /// テンプレートファイルのパス。
        /// とりあえずexeと同階層に決め打ち。
        /// </summary>
        static string TemplateFilePath = Path.Combine(
            Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
            "Template.cs");


        static string TestFileTemplate =
@"using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace NAME_SPACE
{
TEST_CLASSES
}
";

        static string TestClasTemplate =
@"    [TestClass]
    public class CLASS_NAME
    {
        private void AssertIO(string input, string output)
        {
            Console.SetIn(new StringReader(input));
            StringWriter writer = new StringWriter();
            Console.SetOut(writer);
            PROBLEM_NAME.Main(null);
            Assert.AreEqual(output + Environment.NewLine, writer.ToString());
        }

TEST_METHODS
    }
";

        static string TestMethodTemplate =
@"        [TestMethod]
        public void METHOD_NAME()
        {
            string input =
@""INPUT"";
            string output =
@""OUTPUT"";
            AssertIO(input, output);
        }
";
        
        /// <summary>
        /// 改行コードにマッチする正規表現（改行コードを統一するために使う）
        /// </summary>
        static Regex NewlinePattern = new Regex(@"\r\n|\r|\n", RegexOptions.Compiled);

        /// <summary>
        /// atcoder-cliでダウンロードしてきたin/outファイルを一つのMSテストのファイルに纏める。
        /// </summary>
        /// <param name="json"></param>
        static void CreateTestFile(dynamic json)
        {
            List<string> classes = new List<string>();
            foreach(var task in json.tasks)
            {
                string testDirPath = Path.Combine(task.directory.path, task.directory.testdir);
                string[] inputFilePaths = Directory.GetFiles(testDirPath, "*.in");
                string[] outputFilePaths = new string[inputFilePaths.Length];
                string[] testNames = new string[inputFilePaths.Length];
                Array.Sort(inputFilePaths);

                // inputファイルに対応するoutputファイルが存在するかチェックする
                for (int i = 0; i < inputFilePaths.Length; i++)
                {
                    string tmp = Path.GetFileNameWithoutExtension(inputFilePaths[i]);
                    outputFilePaths[i] = Path.Combine(testDirPath, tmp + ".out");
                    if (!File.Exists(outputFilePaths[i]))
                    {
                        throw new FileNotFoundException("Output file was not found.", outputFilePaths[i]);
                    }
                    testNames[i] = tmp.Replace("-", "_");
                }

                // テストメソッドを生成
                List<string> methods = new List<string>();
                for (int i = 0; i < inputFilePaths.Length; i++)
                {
                    string methodStr = TestMethodTemplate;
                    methodStr = methodStr.Replace("METHOD_NAME", testNames[i]);
                    methodStr = methodStr.Replace("INPUT", File.ReadAllText(inputFilePaths[i]));
                    methodStr = methodStr.Replace("OUTPUT", File.ReadAllText(outputFilePaths[i]));
                    methods.Add(methodStr);
                }

                // テストクラスを生成
                string label = task.label.ToUpper();
                string classStr = TestClasTemplate;
                classStr = classStr.Replace("CLASS_NAME", label + "Test");
                classStr = classStr.Replace("PROBLEM_NAME", label);
                classStr = classStr.Replace("TEST_METHODS", string.Join("\n", methods));
                classes.Add(classStr);
            }
            
            // ネームスペースとusingを追加
            string contestName = json.contest.id.ToUpper();
            string testFileData = TestFileTemplate;
            testFileData = testFileData.Replace("NAME_SPACE", contestName);
            testFileData = testFileData.Replace("TEST_CLASSES", string.Join("\n",classes));
            testFileData = NewlinePattern.Replace(testFileData, "\r\n");

            // ファイルとして書き出す
            // もしファイルが既に存在していたら上書きする
            string testFilePath = Path.Combine(TestFileDir, contestName + "Test.cs");
            File.WriteAllText(testFilePath, testFileData);

            Console.WriteLine($"CREATED : {testFilePath}");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        static void CopyTemplate(dynamic json)
        {
            if (!File.Exists(TemplateFilePath))
            {
                Console.WriteLine(
@$"Template file was not found.
path: {TemplateFilePath}");
            }

            foreach (var task in json.tasks)
            {
                string contestName = json.contest.id.ToUpper();
                string problemName = task.label.ToUpper();
                string template = File.ReadAllText(TemplateFilePath);
                template = template.Replace("CONTEST_NAME", contestName);
                template = template.Replace("PROBLEM_NAME", problemName);

                // 作業ディレクトリに書き込み
                string filePath = Path.Combine(
                    Environment.CurrentDirectory,
                    problemName + ".cs"
                    );

                if (File.Exists(filePath))
                {
                    Console.WriteLine($"SKIPPED (already exists) : {filePath}");
                    continue;
                }

                File.WriteAllText(filePath, template);
                Console.WriteLine($"CREATED : {filePath}");
            }
        }

        /// <summary>
        /// json文字列をパースしてExpandObjectとして返す。
        /// 標準のJsonSerializer.Deserialize＜System.Dynamic.ExpandoObject＞だと1層目しか
        /// ExpandObjectにならず、2階層目以降はJsonElementになる。
        /// このメソッドの戻り値は2層目以降もEpandObjectになる。
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        static ExpandoObject Parse(string json)
        {
            using var document = JsonDocument.Parse(json);
            return toExpandoObject(document.RootElement);

            object propertyValue(JsonElement elm) =>
                elm.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.Number => elm.GetDecimal(),
                    JsonValueKind.String => elm.GetString(),
                    JsonValueKind.False => false,
                    JsonValueKind.True => true,
                    JsonValueKind.Array => elm.EnumerateArray().Select(propertyValue).ToArray(),
                    _ => toExpandoObject(elm),
                };

            ExpandoObject toExpandoObject(JsonElement elm) 
            {
                var exo = new ExpandoObject();
                foreach(var prop in elm.EnumerateObject())
                {
                    ((IDictionary<string,object>)exo).Add(prop.Name, propertyValue(prop.Value));
                }
                return exo;
            };
        }
    }
}
