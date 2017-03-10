# ChatRoom
これは簡易なチャットルームを実現するプログラムです。</br>
「Release」フォルダー内はビルド済みの実行ファイルです。</br>

開発環境: 
  Visual Studio2015 + C# + .Net Framework4.5</br>
  </br>
C#でのネット機能とマルチスレッド機能を練習するために作りました。</br>
マルチスレッド機能について、練習のため、Serverはasync awaitの方法を使い、Client側はnew Threadの方法を使いました。</br>

使用説明：</br>
Server側 : </br>
　PortNumberを設定し（デフォルトは55555）、Startを押すとListen開始します。ClientListに現在繋がっているClientの情報を表します。</br>

Client側 : </br>
　複数起動して、username（同じServer上のusernameは重複不可）と対応するIPとPortを入力して、LogInを押すと、Serverと繋がります。</br>
　左下のUserListは現在Room内の自分以外のUserを表します。</br>
　右下のTextBoxに文字を入力して、Sendを押すと送信します。</br>
  ModeのPublicを選択すると全体へ送信します。Privを選択すると、左下のUserList中選択した相手だけに送信します。</br>

