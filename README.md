# これは何？
C# / Visual Studio / MSTest というニッチな環境向けのAtCoderテストケース自動作成ツールです。  
コマンド1発でMSTest用のテストファイルとテンプレートを準備してくれます。
atcoder-cli の WRAPPER なのでWSLのインストールが必要です。

# 準備
- WSL上にatcoder-cliとonline-judge-toolsをインストールします。  
参考：  
atcoder-cli インストールガイド  
http://tatamo.81.la/blog/2018/12/07/atcoder-cli-installation-guide/

- atcoder-cliとonline-judge-toolsでAtCoderにログインします。  
  - $ acc login
  - $ oj login https://atcoder.jp/
- acsetupを好きなフォルダに展開します。

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


下記コードを実行するとカレントディレクトリにABC101フォルダが作られ、サンプルデータがDLされる。
また、そのデータを元にMSTest用のテストコードが生成され、指定したフォルダに出力される。
そして、accsetup.exeと同じディレクトリに存在するTemplate.csが、コンテスト名や問題名の置換処理を経て、指定したフォルダに出力される。  

初回実行時はatcoder-cliがatcoderのログインIDとパスワードを聞いてくる（ローカルにはセッション情報が保存される）。
```
$ acsetup ABC101
```



# 発展的な使い方
- .bashrcに下記関数を追加し、フォルダ名を適宜変更する  
--tmpl-destはテンプレートの出力先フォルダを指定  
--test-destはMSTest用のテストファイルの出力先フォルダを指定

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

