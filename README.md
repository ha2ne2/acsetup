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
$ acc new --choice all abc100
```

2.  次にacsetupを実行します。acsetupはatcoder-cliの設定ファイルを読んでよきに計らってくれます。
```
$ cd abc100
$ /mnt/c/Users/ha2ne2/opt/acsetup/acsetup.exe
CREATED : .\abc100_a_Test.cs
CREATED : .\abc100_b_Test.cs
CREATED : .\abc100_c_Test.cs
CREATED : .\abc100_d_Test.cs
CREATED : .\abc100_a.cs
CREATED : .\abc100_b.cs
CREATED : .\abc100_c.cs
CREATED : .\abc100_d.cs
```


# 発展的な使い方

サンプルケースのDL、テストファイルの作成、テンプレートの展開、プロジェクトの作成、ソリューションへの追加 ・・・  
**コマンド1発で全てやりたい！**



そんな時は.bashrcに下記関数を追加すると出来ます。
SOLUTION_DIRは.slnファイルのあるフォルダを指定して下さい。  

```
readonly SOLUTION_DIR="c:/Users/ha2ne2/GitRepos/ABC"

function acready() {
    dotnet.exe new console -o $SOLUTION_DIR/$1
    dotnet.exe add $SOLUTION_DIR/$1 package Microsoft.Net.Test.Sdk
    dotnet.exe add $SOLUTION_DIR/$1 package MSTest.TestAdapter
    dotnet.exe add $SOLUTION_DIR/$1 package MSTest.TestFramework
    dotnet.exe sln $SOLUTION_DIR add $SOLUTION_DIR/$1
}

function acsetup() {
    acc new --choice all $1
    if [ -e $1 ]; then
        # create project and add to solution
        if [ ! -e $(wslpath -u $SOLUTION_DIR/$1/$1.csproj) ]; then
            acready $1
        fi
        cd $1
        command /mnt/c/Users/ha2ne2/opt/acsetup/acsetup.exe \
            --tmpl-dest $SOLUTION_DIR/$1 \
            --test-dest $SOLUTION_DIR/$1/Tests
        cd ../
    fi
}
```


設定後、.bashrcを再読込すると、下記コマンドでサンプルケースのDLからソリューション関係まですべてやってくれるようになります。
```
$ acsetup abc100
```

出力は下記のように構成されます。
<pre>
ソリューション 'ABC'
├ abc100
│├ abc100_a.cs
│├ abc100_b.cs
│├ abc100_c.cs
│├ abc100_d.cs
│└ Tests
│　├ abc100_a_Test.cs
│　├ abc100_b_Test.cs
│　├ abc100_c_Test.cs
│　└ abc100_d_Test.cs
├ abc101
│├ abc101_a.cs
│├ abc101_b.cs
│├ abc101_c.cs
│├ abc101_d.cs
│└ Tests
│　├ abc101_a_Test.cs
│　├ abc101_b_Test.cs
│　├ abc101_c_Test.cs
│　└ abc100h1_d_Test.cs
│
</pre>

# 出力されるテンプレートについて
テンプレートは、acsetupと同じフォルダにあるTemplate.csを元に、置換処理が行われた後出力されます。  
Template.csの中にある特定のワードが置換され出力されるイメージです。  
置換処理は次のテーブルを元に行われます。  

| 置換前 | 置換後 |
----|---- 
| CONTEST_ID | コンテストID（abc100 など）（ハイフンはアンダーバーに置換されます） |
| CONTEST_ID_ORIG |  コンテスト名（abc100 など）（ハイフンはアンダーバーに置換されません） |
| TASK_ID | タスクID（abc100_a など（URL末尾部分）） |
| TASK_LABEL | タスクラベル（A, B, C など） |
| TASK_TITLE | タスクのタイトル（Walking Takahashi など）
| TASK_URL | 問題のURL（https://atcoder.jp/contests/Judge-Update-202004/tasks/judge_update_202004_a など） |

Template.csは自分好みに自由に編集できます。

# 出力されるテストファイルについて

サンプルケースを元に下記のようなファイルを生成します。  
設定ファイルによるカスタマイズは今の所出来ません。  
カスタマイズしたい場合はソースコードに埋め込まれているボイラープレートを変更してリビルドして下さい。

出力例
```
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;

namespace abc100.Tests
{
    [TestClass]
    public class abc100_a_Test
    {
        [TestMethod]
        public void abc100_a_sample_1()
        {
            string input =
@"5 4
";
            string output =
@"Yay!
";
            AssertIO(input, output);
        }

        [TestMethod]
        public void abc100_a_sample_2()
        {
            string input =
@"8 8
";
            string output =
@"Yay!
";
            AssertIO(input, output);
        }

        [TestMethod]
        public void abc100_a_sample_3()
        {
            string input =
@"11 4
";
            string output =
@":(
";
            AssertIO(input, output);
        }

        private void AssertIO(string input, string output)
        {
            Console.SetIn(new StringReader(input));
            StringWriter writer = new StringWriter();
            Console.SetOut(writer);
            abc100_a.Program.Main(null);
            Assert.AreEqual(output, writer.ToString());
        }
    }
}

```
