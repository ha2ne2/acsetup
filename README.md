# これは何？
C# / Visual Studio / MSTest というニッチな環境向けのAtCoderテストケース自動作成ツールです。  
コマンド1発でMSTest用のテストファイルの作成と、問題別のテンプレートを展開をしてくれます。  
atcoder-cli の WRAPPER なのでWSLのインストールが必要です。

# 準備
- WSL上にatcoder-cliとonline-judge-toolsをインストールします。  
参考：  
atcoder-cli インストールガイド  
http://tatamo.81.la/blog/2018/12/07/atcoder-cli-installation-guide/

- atcoder-cliとonline-judge-toolsでAtCoderにログインします。  
  - $ acc login
  - $ oj login https://atcoder.jp/
- acsetupをビルドして好きなフォルダに設置します。C# 8がビルドできる環境が必要です（Visual Studio 2019など）。

# 使い方

例としてABC101のテストファイルとテンプレートを作成していく手順を示します。  
1. まずatcoder-cliでサンプルケースをDLします。
```
$ acc new --choice all ABC101
```

2.  次にacsetupを実行します。acsetupはatcoder-cliの設定ファイルを読んでよきに計らってくれます。
```
$ cd ABC101
$ /mnt/c/Users/ha2ne2/opt/acsetup/acsetup.exe
CREATED : .\ABC101Test.cs
CREATED : .\A.cs
CREATED : .\B.cs
CREATED : .\C.cs
CREATED : .\D.cs
```


# 発展的な使い方

サンプルケースのDL、テストファイルの作成、テンプレートの展開、プロジェクトの作成、ソリューションへの追加 ・・・  
**コマンド1発で全てやりたい！**



そんな時は.bashrcに下記関数を追加すると出来ます。フォルダ名は適宜変更してください。  
--tmpl-destはテンプレートの出力先フォルダを指定して下さい  
--test-destはMSTest用のテストファイルの出力先フォルダを指定して下さい

```
function acsetup() {
    acc new --choice all $1
    if [ -e $1 ]; then
        # create project and add to solution
        dotnet.exe new console -o c:/Users/ha2ne2/GitRepos/ABC/$1
        dotnet.exe sln c:/Users/ha2ne2/GitRepos/ABC/ add c:/Users/ha2ne2/GitRepos/ABC/$1
        cd $1
        command /mnt/c/Users/ha2ne2/opt/acsetup/acsetup.exe \
            --tmpl-dest c:/Users/ha2ne2/GitRepos/ABC/$1 \
            --test-dest c:/Users/ha2ne2/GitRepos/ABC/UnitTest
        cd ../
    fi
}
```


設定後、.bashrcを再読込すると、下記コマンドでサンプルケースのDLからソリューション関係まですべてやってくれるようになります。
```
$ acsetup ABC100
```

# 出力されるテンプレートについて
テンプレートは、acsetupと同じフォルダにあるTemplate.csを元に、置換処理が行われた後出力されます。  
Template.csの中にある特定のワードが置換され出力されるイメージです。  
置換処理は次のテーブルを元に行われます。  

| 置換前 | 置換後 |
----|---- 
| CONTEST_NAME | コンテスト名（ABC100 など）（ハイフンはアンダーバーに置換されます） |
| CONTEST_NAME_ORIG |  コンテスト名（ABC100 など）（ハイフンはアンダーバーに置換されません） |
| PROBLEM_LABEL | 問題のラベル（A, B, C など） |
| PROBLEM_TITLE | 問題のタイトル（Walking Takahashi など）
| PROBLEM_URL | 問題のURL（https://atcoder.jp/contests/Judge-Update-202004/tasks/judge_update_202004_a など） |

Template.csは自分好みに自由に編集できます。

# 出力されるテストファイルについて

サンプルケースを元に下記のようなファイルを生成します。  
カスタマイズは今の所出来ません。

出力例
```
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace ABC100
{
    [TestClass]
    public class ATest
    {
        [TestMethod]
        public void sample_1()
        {
            string input =
@"+-++
";
            string output =
@"2
";
            AssertIO(input, output);
        }

        [TestMethod]
        public void sample_2()
        {
            string input =
@"-+--
";
            string output =
@"-2
";
            AssertIO(input, output);
        }

        [TestMethod]
        public void sample_3()
        {
            string input =
@"----
";
            string output =
@"-4
";
            AssertIO(input, output);
        }


        private void AssertIO(string input, string output)
        {
            Console.SetIn(new StringReader(input));
            StringWriter writer = new StringWriter();
            Console.SetOut(writer);
            A.Program.Main(null);
            Assert.AreEqual(output + Environment.NewLine, writer.ToString());
        }
    }

    [TestClass]
    public class BTest
    {
        [TestMethod]
        public void sample_1()
        {
            string input =
@"12
";
            string output =
@"Yes
";
            AssertIO(input, output);
        }

        （中略）

        private void AssertIO(string input, string output)
        {
            Console.SetIn(new StringReader(input));
            StringWriter writer = new StringWriter();
            Console.SetOut(writer);
            B.Program.Main(null);
            Assert.AreEqual(output + Environment.NewLine, writer.ToString());
        }
    }

    ...
}
```
