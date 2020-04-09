# これは何？
C# / Visual Studio / MSTest というニッチな環境向けのAtCoderテストケース自動作成ツールです。  
コマンド1発でMSTest用のテストファイルの作成と、問題別のテンプレートを展開をしてくれます。  
コンテスト毎にプロジェクトを作成し1つのソリューションで管理という構成には特に威力を発揮します。  
atcoder-cli に依存しているのでWSLのインストールが必要です。

# 準備
- WSL上にatcoder-cliとonline-judge-toolsをインストールします。  
参考： atcoder-cli インストールガイド  
http://tatamo.81.la/blog/2018/12/07/atcoder-cli-installation-guide/

- atcoder-cliとonline-judge-toolsでAtCoderにログインします。  
  ```
  $ acc login
  $ oj login https://atcoder.jp/
  ```
- acsetupをビルドして好きなフォルダに配置します。C# 8がビルドできる環境が必要です（Visual Studio 2019など）。

# 使い方

例として ABC100 のテストファイルとテンプレートを作成していく手順を示します。  
1. まず atcoder-cli でサンプルケースをDLします。
   ```
   $ acc new --choice all abc100
   ```

2. 次に acsetup を実行します。acsetup は atcoder-cli の生成するコンテスト情報ファイルを読み、タスク毎にテストファイルとテンプレートを生成します。
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

コンテスト毎にプロジェクトを作成し1つのソリューションで管理している場合、コンテスト参加の準備にかかる手間が意外と馬鹿になりません。  
サンプルケースのDL、テストファイルの作成、テンプレートの展開、プロジェクトの作成、既存のソリューションへの追加 ・・・  
  

**コマンド1発で全てやりたい！**
  

そんな時は.bashrcに下記関数を追加すると出来ます。  
一行目のSOLUTION_DIRには.slnファイルのあるフォルダを指定して下さい。  

```
readonly SOLUTION_DIR="c:/Users/ha2ne2/GitRepos/ABC"

##
## ヘルパ関数です。
## プロジェクトの作成、MSTestに必要な依存関係の解決、ソリューションへの追加を行います。
##
function acprepare() {
    local readonly project_name=$1

    # create project
    dotnet.exe new classlib -o $SOLUTION_DIR/$project_name

    # add nuget package to project to test
    dotnet.exe add $SOLUTION_DIR/$project_name package Microsoft.Net.Test.Sdk
    dotnet.exe add $SOLUTION_DIR/$project_name package MSTest.TestAdapter
    dotnet.exe add $SOLUTION_DIR/$project_name package MSTest.TestFramework

    # add project to solution
    dotnet.exe sln $SOLUTION_DIR add $SOLUTION_DIR/$project_name
}

##
## 指定したコンテストに含まれるタスクを全て解きたい場合に使用します。
## コンテストIDを名前としたプロジェクトが新しく作成され、
## SOLUTION_DIRで指定したソリューションに追加されます。
##
## Usage   : acsetup contest_id
## Example : acsetup abc100
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
## 生成されたテストとテンプレートは、引数で指定されたプロジェクトに追加されます。
## 指定されたプロジェクトが存在しない場合は新規作成され、
## SOLUTION_DIRで指定したソリューションに追加されます。
##
## Usage   : acsetup1 project_name [sub_dir_name] task_url
## Example :
## acsetup1 20200408 https://atcoder.jp/contests/abc106/tasks/abc106_a
## => 20200408プロジェクトにabc106_a.csが追加されます
##
## acsetup1 202004 20200408 https://atcoder.jp/contests/abc106/tasks/abc106_a
## => 202004プロジェクトの20200408フォルダにabc106_a.csが追加されます
##
function acsetup1(){
    local project_name
    local sub_dir_name
    local url
    if [[ $# -eq 2 ]]; then
        project_name=$1
        local url=$2
    elif [[ $# -eq 3 ]]; then
        project_name=$1
        sub_dir_name=$2
         url=$3
    else
        echo 'invalid args'
        return 1
    fi

    local path=$project_name
    if [[ -n "$sub_dir_name" ]]; then
        path=$path/$sub_dir_name
    fi
    
    if [[ $url =~ /([^/]+)/tasks/([^/]+)$ ]]; then
        local readonly contest_id=${BASH_REMATCH[1]}
        local readonly task_id=${BASH_REMATCH[2]}
        local readonly tmp_dir=$(mktemp -d)

        pushd $tmp_dir
        acc new --choice all --no-tests --task-dirname-format {TaskID} $contest_id
        if [ -e $contest_id ]; then # when acc new suceeded
            # download sample into target task directory in tmp_dir
            cd $contest_id/$task_id
            oj download $url
            cd ../
            
            # create project when project doesn't exist
            if [ ! -e $(wslpath -u $SOLUTION_DIR/$project_name) ]; then
                acprepare $project_name
            fi

            # create test and template
            command /mnt/c/Users/ha2ne2/opt/acsetup/acsetup.exe \
                --tmpl-dest $SOLUTION_DIR/$path \
                --test-dest $SOLUTION_DIR/$path/Tests
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
'ABC' ソリューション 
├ abc100 プロジェクト
│├ abc100_a.cs
│├ abc100_b.cs
│├ abc100_c.cs
│├ abc100_d.cs
│└ Tests フォルダ
│　├ abc100_a_Test.cs
│　├ abc100_b_Test.cs
│　├ abc100_c_Test.cs
│　└ abc100_d_Test.cs
├ abc101 プロジェクト
│├ abc101_a.cs
│├ abc101_b.cs
│├ abc101_c.cs
│├ abc101_d.cs
│└ Tests フォルダ
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
