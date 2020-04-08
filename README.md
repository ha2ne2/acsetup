# これは何？
C# / Visual Studio / MSTest というニッチな環境向けのAtCoderテストケース自動作成ツールです。  
コマンド1発でMSTest用のテストファイルの作成と、問題別のテンプレートを展開をしてくれます。  
atcoder-cli に依存しているのでWSLのインストールが必要です。

# 準備
- WSL上にatcoder-cliとonline-judge-toolsをインストールします。  
参考：  
atcoder-cli インストールガイド  
http://tatamo.81.la/blog/2018/12/07/atcoder-cli-installation-guide/

- atcoder-cliとonline-judge-toolsでAtCoderにログインします。  
  - $ acc login
  - $ oj login https://atcoder.jp/
- acsetupをビルドして好きなフォルダに配置します。C# 8がビルドできる環境が必要です（Visual Studio 2019など）。

# 使い方

例として ABC100 のテストファイルとテンプレートを作成していく手順を示します。  
1. まず atcoder-cli でサンプルケースをDLします。
```
$ acc new --choice all abc100
```

2.  次に acsetup を実行します。acsetup は atcoder-cli の生成するコンテスト情報ファイルを読み、タスク毎にテストファイルとテンプレートを生成します。
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

コンテスト毎にプロジェクトを作成し1つのソリューションで管理している場合、コンテスト開催時の準備にかかる手間が意外と馬鹿になりません。  
サンプルケースのDL、テストファイルの作成、テンプレートの展開、プロジェクトの作成、既存のソリューションへの追加 ・・・  
  

**コマンド1発で全てやりたい！**
  

そんな時は.bashrcに下記関数を追加すると出来ます。  
SOLUTION_DIRには.slnファイルのあるフォルダを指定して下さい。  

```

readonly SOLUTION_DIR="c:/Users/ha2ne2/GitRepos/ABC"

##
## create project / add nuget package / add project to solution
##
function acprepare() {
    # create project
    dotnet.exe new classlib -o $SOLUTION_DIR/$1
    # add nuget package to project to test
    dotnet.exe add $SOLUTION_DIR/$1 package Microsoft.Net.Test.Sdk
    dotnet.exe add $SOLUTION_DIR/$1 package MSTest.TestAdapter
    dotnet.exe add $SOLUTION_DIR/$1 package MSTest.TestFramework
    # add project to solution
    dotnet.exe sln $SOLUTION_DIR add $SOLUTION_DIR/$1
}

##
## 指定したコンテストに含まれるタスクを全て解きたい場合に使用します。
## 引数で指定したコンテストに含まれる全てのタスクに対して、テストとテンプレートを生成します。
## 引数で指定したコンテストを名前としたプロジェクトを生成し、そのテストとテンプレートを追加します。
## SOLUTION_DIRで指定したソリューションに、そのプロジェクトを追加します。
## usage) $ acsetup abc001
## 
function acsetup() {
    local readonly contest_id=$1
    acc new --choice all $contest_id
    if [ -e $contest_id ]; then # when acc new suceeded
        # create project when project doesn't exist
        if [ ! -e $(wslpath -u $SOLUTION_DIR/$contest_id) ]; then
            acprepare $contest_id
        fi
        cd $contest_id
        command /mnt/c/Users/ha2ne2/opt/acsetup/acsetup.exe \
            --tmpl-dest $SOLUTION_DIR/$contest_id \
            --test-dest $SOLUTION_DIR/$contest_id/Tests
        cd ../
    fi
}

##
## タスクを1問だけ指定して解きたい場合に使用します。
## 第二引数でURLとして指定した一つのタスクに対して、テストとテンプレートを生成します。
## 第一引数を名前としたプロジェクトに対し、そのテストとテンプレートを追加します。
## 第一引数を名前としたプロジェクトが存在しない場合はプロジェクトを生成します。
## SOLUTION_DIRで指定したソリューションに、そのプロジェクトを追加します。
## usage) $ acsetup1 20200408 https://atcoder.jp/contests/abc106/tasks/abc106_a
## 
function acsetup1(){
    local readonly project_name=$1
    local readonly url=$2
    
    if [[ $url =~ /([^/]+)/tasks/([^/]+)$ ]]; then
        contest_id=${BASH_REMATCH[1]}
        task_id=${BASH_REMATCH[2]}
        tmp_dir=$(mktemp -d)
        pushd $tmp_dir
        acc new --choice all --no-tests --task-dirname-format {TaskID} $contest_id
        if [ -e $contest_id ]; then # when acc new suceeded
            cd $contest_id/$task_id
            oj download $url # download sample into target task directory

            # create project when project doesn't exist
            if [ ! -e $(wslpath -u $SOLUTION_DIR/$project_name) ]; then
                acprepare $project_name
            fi
            cd ../
            command /mnt/c/Users/ha2ne2/opt/acsetup/acsetup.exe \
                --tmpl-dest $SOLUTION_DIR/$project_name \
                --test-dest $SOLUTION_DIR/$project_name/Tests
            cd ../
        fi
        popd
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
│　└ abc101_d_Test.cs
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
