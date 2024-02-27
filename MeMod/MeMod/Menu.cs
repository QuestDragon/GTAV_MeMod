using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using GTA;
using GTA.UI;
using NativeUI;

namespace MeMod
{
    internal class Menu
    {
        static XElement xml = null; //XML File
        private static UIMenuItem rmtarget = null;
        public static MenuPool modMenuPool; //メニューUIの土台
        static UIMenu mainMenu; //メニューUIのフレーム
        static UIMenu settingsMenu; //設定メニューUIのフレーム
        static UIMenu removeMemos; //サブメニューとして使うフレーム。中身はSetupメソッドで作る。型はMainMenuと同じ。

        public static Dictionary<string, bool> MemoState = new Dictionary<string, bool>();

        static List<dynamic> pos = new List<dynamic>
        {
            "Subtitle",
            "~y~Help message",
            "~y~Notification"
        };

        //個別でカスタマイズ等を行う場合や選択された項目によって処理を変える場合はオブジェクトを作ってフレームに追加する形になる。特にその予定がないならAddメソッドにNew構文で追加してしまって構わない。
        static UIMenuItem add = new UIMenuItem("Add memo", "Add a memo item."); //メニューUIに表示するアイテムオブジェクト。
        static UIMenuItem allreset = new UIMenuItem("~r~Reset memo");
        // static UIMenuCheckboxItem autoReset = new UIMenuCheckboxItem("Reset memo at end of Traffic Stop", false, "Initializes the state of the memo when Traffic Stop is terminated."); //メニューUIに表示するアイテムオブジェクト。チェックボックスアイテム(アイテム名, 初期値をT/Fで指定)
        public static UIMenuCheckboxItem showing = new UIMenuCheckboxItem("Always display memo", false, "The memo status is always displayed on the right side of the screen.");
        static UIMenuCheckboxItem only_changed = new UIMenuCheckboxItem("Show only notes that have been changed", false, "Only those memo items that have changed from their default state will be displayed.");
        static UIMenuCheckboxItem show_left = new UIMenuCheckboxItem("Shown on the left", false, "Displays notes on the left side of the screen.");
        // static UIMenuListItem showPos = new UIMenuListItem("Display memo in next position", pos, 0); // 30f選択状態


        /// <summary>
        /// メニュー表示切り替え
        /// </summary>
        public static void show()
        {
            if(!mainMenu.Visible) //表示される前にメニューを更新
            {
                UpdateMain();
            }
            mainMenu.Visible = !mainMenu.Visible;
        }

        /// <summary>
        /// 文字列のTrueFalseをBool型に変換
        /// </summary>
        /// <param name="s">TrueまたはFalseが格納された文字列</param>
        /// <returns>Bool型（True / False）。ただし、Trueでない文字列はFalseが返る。</returns>
        public static bool TorF(string s)
        {
            if (s.ToLower() == "true")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /*
        static int SelectPosList(string s)
        {
            if (int.TryParse(s, out int pos)) //数値である
            {
                if (int.Parse(s) > 2) //範囲を超えている場合
                {
                    return 0;
                }
                else
                {
                    return int.Parse(s);
                }
            }
            else
            {
                return 0;
            }
        }
        */

        /// <summary>
        /// 初回実行用
        /// </summary>
        /// <returns>ロードしたメモの数</returns>
        public static int CreateMenu()
        {
            modMenuPool = new MenuPool(); //土台作成
            mainMenu = new UIMenu("MeMod", "For myself, ~b~Me~s~mo ~b~Mod~s~."); //各メニュー画面の作成
            settingsMenu = new UIMenu("MeMod", "Customize your own settings.");
            removeMemos = new UIMenu("MeMod", "Select the memo to be ~o~deleted~s~.");
            MemoState.Clear(); //初期化

            modMenuPool.Add(mainMenu); //土台にメニューフレーム追加

            settingsMenu = modMenuPool.AddSubMenu(mainMenu, "Settings"); //サブメニューを土台に追加し、このサブメニューにアクセスする親メニューを引数に指定。第2引数には項目名。

            xml = XElement.Load(@"scripts\MeMod\memos.xml");
            //タグ内の情報を取得する(1番親のタグはElementsの引数に書かないので注意！)
            IEnumerable<XElement> infos = from item in xml.Elements("memo")
                                          select item;

            int counter = 0;
            //memos→memo分ループして、アイテム追加
            foreach (XElement info in infos)
            {
                mainMenu.AddItem(new UIMenuCheckboxItem(info.Value, TorF((string)info.Attribute("default"))));
                MemoState.Add(info.Value, TorF((string)info.Attribute("default")));
                counter++;
            }

            mainMenu.AddItem(allreset);

            mainMenu.RefreshIndex(); //項目追加が完了したらリフレッシュ。
            mainMenu.OnCheckboxChange += onMainMenuCheckItemSelect; //チェックボックスアイテムが選択されたときのイベントハンドラを定義。以下にイベントハンドラのVoid型メソッドを作成して項目が選択されたときの処理を書く。
            mainMenu.OnItemSelect += onMainMenuItemSelect;
            mainMenu.OnMenuOpen += onMainMenuOpen;

            CreateSettings();
            CreateRemoveMemos();

            mainMenu.OnIndexChange += (sender, newindex) => //メニュー項目を移動した場合
            {
                if (is_reset_soon != null) //リセットボタンが保存されている場合
                {
                    is_reset_soon.Description = ""; //リセットボタンデータのDescriptionを削除
                    mainMenu.RefreshIndex(); //表示の更新。ただし一番上にフォーカスが戻ってしまうので、選択位置を固定する場合は以下構文のようにUIMenuItemオブジェクトを選択状態にする必要がある。
                    mainMenu.CurrentSelection = newindex; //移動後の項目位置を選択状態にする

                    is_reset_soon = null; //リセットボタンの保存データを削除
                }
            };

            mainMenu.OnMenuChange += (sender, nextmenu, forword) => //メニューを移動した場合
            {
                if (is_reset_soon != null) //リセットボタンが保存されている場合
                {
                    is_reset_soon.Description = ""; //リセットボタンだったデータのDescriptionを削除
                    mainMenu.RefreshIndex(); //表示の更新。ただし一番上にフォーカスが戻ってしまうので、選択位置を固定する場合は以下構文のようにUIMenuItemオブジェクトを選択状態にする必要がある。
                    is_reset_soon = null; //リセットボタンの保存データを削除
                }
            };

            return counter;
        }

        private static void onMainMenuOpen(UIMenu sender)
        {
            UpdateMain();
        }

        static UIMenuItem is_reset_soon = null;
        private static void onMainMenuItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == allreset)
            {
                if (selectedItem.Description == "")
                {
                    is_reset_soon = selectedItem; //対象を保存
                    selectedItem.Description = "~r~Are you sure?";
                    mainMenu.RefreshIndex(); //表示の更新。ただし一番上にフォーカスが戻ってしまうので、選択位置を固定する場合は以下構文のようにUIItemオブジェクトを選択状態にする必要がある。
                    mainMenu.CurrentSelection = index; //選択項目を再設定
                }
                else
                {
                    is_reset_soon = null;
                    mainMenu.CurrentSelection = 0; //一番上にフォーカス
                    show(); //非表示に戻す
                    CreateMenu();
                    Notification.Show("~b~MeMod~s~: Done!");

                }
            }
        }

        /// <summary>
        /// MainMenuの更新
        /// </summary>
        static void UpdateMain()
        {
            mainMenu.Clear(); //初期化

            settingsMenu = modMenuPool.AddSubMenu(mainMenu, "Settings"); //サブメニューを土台に追加し、このサブメニューにアクセスする親メニューを引数に指定。第2引数には項目名。

            xml = XElement.Load(@"scripts\MeMod\memos.xml");
            //タグ内の情報を取得する(1番親のタグはElementsの引数に書かないので注意！)
            IEnumerable<XElement> infos = from item in xml.Elements("memo")
                                          select item;

            //memos→memo分ループして、アイテム追加
            foreach (XElement info in infos)
            {
                mainMenu.AddItem(new UIMenuCheckboxItem(info.Value, MemoState[info.Value]));
            }

            mainMenu.AddItem(allreset);
            mainMenu.RefreshIndex(); //項目追加が完了したらリフレッシュ。

            CreateSettings();
            CreateRemoveMemos();
        }

        private static void onMainMenuCheckItemSelect(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool Checked)
        {
            MemoState[checkboxItem.Text] = Checked; //状態を保存
            if (showing.Checked)
            {
                Class1.setText(content_gen(), show_left.Checked);
            }

        }

        /// <summary>
        /// SettingsMenuの作成
        /// </summary>
        static void CreateSettings()
        {
            settingsMenu.Clear(); //初期化

            settingsMenu.AddItem(add);
            removeMemos = modMenuPool.AddSubMenu(settingsMenu, "Remove memo");
            settingsMenu.AddItem(showing);
            settingsMenu.AddItem(only_changed);
            settingsMenu.AddItem(show_left);

            //設定内容の適用

            // autoReset.Checked = Class1.ini.GetValue<bool>("Handler", "LSPDFRAutoReset", false);
            showing.Checked = Class1.ini.GetValue<bool>("Parameters", "AlwaysShow", false);
            only_changed.Checked = Class1.ini.GetValue<bool>("Parameters", "OnlyChanged", false);
            show_left.Checked = Class1.ini.GetValue<bool>("Parameters", "ShowLeft", false);

            only_changed.Enabled = showing.Checked;
            show_left.Enabled = showing.Checked;

            settingsMenu.RefreshIndex();

            settingsMenu.OnCheckboxChange += onSettingsCheckItemSelect; //チェックボックスアイテムが選択されたときのイベントハンドラを定義。以下にイベントハンドラのVoid型メソッドを作成して項目が選択されたときの処理を書く。
            // settingsMenu.OnListChange += onSettingListChange;
            settingsMenu.OnItemSelect += onSettingItemSelect;

            if (showing.Checked)
            {
                Class1.setText(content_gen(), show_left.Checked);
            }

        }

        private static void xml_add(string content)
        {
            xml = XElement.Load(@"scripts\MeMod\memos.xml");
            //新しいメンバー情報を設定する
            XElement datas = new XElement("memo", content,
            new XAttribute("default", "False")); //属性追加

            //情報を追加する
            xml.Add(datas);

            //追加した情報を保存する
            xml.Save(@"scripts\MeMod\memos.xml");

            MemoState.Add(content, false);
        }

        private static string content_gen()
        {
            string return_data = "";

            string memo_color = Class1.ini.GetValue<string>("Colors", "Memo", "~s~");
            string true_color = Class1.ini.GetValue<string>("Colors", "True", "~r~");
            string false_color = Class1.ini.GetValue<string>("Colors", "False", "~g~");

            foreach (UIMenuItem item in mainMenu.MenuItems)
            {
                if (!MemoState.ContainsKey(item.Text))
                {
                    continue;
                }
                else if(only_changed.Checked && MemoState[item.Text] == same_default(item.Text))
                {
                    continue; //処理スキップ
                }
                // T/Fによって色分けして文字列に格納
                if (MemoState[item.Text] == true)
                {
                    return_data += $"{memo_color}{item.Text}: {true_color}{MemoState[item.Text]}~n~";
                }
                else
                {
                    return_data += $"{memo_color}{item.Text}: {false_color}{MemoState[item.Text]}~n~";
                }
                
            }
            return return_data;
        }

        private static bool same_default(string target)
        {
            //タグ内の情報を取得する(1番親のタグはElementsの引数に書かないので注意！)
            IEnumerable<XElement> infos = from item in xml.Elements("memo")
                                          select item;

            //ループして、チェック
            foreach (XElement info in infos)
            {
                if (info.Value == target)
                {
                    return TorF((string)info.Attribute("default"));
                }
            }
            return false;
        }

        private static void onSettingItemSelect(UIMenu sender, UIMenuItem selectedItem, int index)
        {
            if (selectedItem == add) //メモの追加時
            {
                WindowTitle title = WindowTitle.EnterMessage20; //メッセージを入力（20文字まで）
                string memo_content = Game.GetUserInput(title, "", 20); //入力フォームを表示。チートコード入力画面的なやつ。入力完了後、以下の処理に移動する

                if (!MemoState.ContainsKey(memo_content))
                {
                    xml_add(memo_content);
                    Notification.Show("~b~MeMod~s~: Added!");
                }
                else
                {
                    Notification.Show("~b~MeMod~s~: Already exist!");
                }
                
            }

        }
        /*
        private static void onSettingListChange(UIMenu sender, UIMenuListItem listItem, int newIndex)
        {
            if (listItem == showPos)
            {
                ScriptSettings ini = ScriptSettings.Load(@"scripts\MeMod.ini");
                //iniに書き込み。
                ini.SetValue("Parameters", "ShowPos", newIndex);
                ini.Save(); //保存処理
            }
        }
        */

        private static void onSettingsCheckItemSelect(UIMenu sender, UIMenuCheckboxItem checkboxItem, bool Checked)
        {
            /*
            if (checkboxItem == autoReset)
            {
                //iniに書き込み。
                Class1.ini.SetValue("Handler", "LSPDFRAutoReset", Checked);
                Class1.ini.Save(); //保存処理
                Class1.reset = Checked;
            }
            */

            if (checkboxItem == showing)
            {
                //iniに書き込み。
                Class1.ini.SetValue("Parameters", "AlwaysShow", Checked);
                Class1.ini.Save(); //保存処理
                only_changed.Enabled = Checked;
                show_left.Enabled = Checked;
            }
            else if (checkboxItem == only_changed)
            {
                //iniに書き込み。
                Class1.ini.SetValue("Parameters", "OnlyChanged", Checked);
                Class1.ini.Save(); //保存処理
            }
            else if (checkboxItem == show_left)
            {
                //iniに書き込み。
                Class1.ini.SetValue("Parameters", "ShowLeft", Checked);
                Class1.ini.Save(); //保存処理
            }
            if (showing.Checked)
            {
                Class1.setText(content_gen(), show_left.Checked);
            }
            else
            {
                Class1.remText();
            }

        }

        /// <summary>
        /// RemoveMemosの作成
        /// </summary>
        static void CreateRemoveMemos()
        {
            removeMemos.Clear(); //初期化

            xml = XElement.Load(@"scripts\MeMod\memos.xml");
            //タグ内の情報を取得する(1番親のタグはElementsの引数に書かないので注意！)
            IEnumerable<XElement> infos = from item in xml.Elements("memo")
                                          select item;

            //memos→memo分ループして、アイテム追加
            foreach (XElement info in infos)
            {
                removeMemos.AddItem(new UIMenuItem(info.Value));
            }
            removeMemos.RefreshIndex(); //項目追加が完了したらリフレッシュ。

            removeMemos.OnItemSelect += (sender, item, index) =>
            {
                if (item.Description == "")
                {
                    rmtarget = item; //削除対象を保存
                    item.Description = "~r~Are you sure?";
                    removeMemos.RefreshIndex(); //表示の更新。ただし一番上にフォーカスが戻ってしまうので、選択位置を固定する場合は以下構文のようにUIItemオブジェクトを選択状態にする必要がある。
                    removeMemos.CurrentSelection = index; //選択項目を再設定
                }
                else
                {

                    xml = XElement.Load(@"scripts\MeMod\memos.xml");
                    //タグ内の情報を取得する(1番親のタグはElementsの引数に書かないので注意！)
                    IEnumerable<XElement> datas = from data in xml.Elements("memo")
                                                  select data;

                    //memos→memo分ループして対象を探す
                    foreach (XElement data in datas)
                    {
                        if(data.Value == item.Text)
                        {
                            data.Remove();
                            //情報を保存する
                            xml.Save(@"scripts\MeMod\memos.xml");

                            break;
                        }
                    }
                    MemoState.Remove(item.Text);

                    rmtarget = null; //削除対象を削除
                    removeMemos.RemoveItemAt(index); //同じUIメニューからもインデックス番号で指定して削除
                    removeMemos.RefreshIndex();
                    removeMemos.CurrentSelection = index; //削除後の選択項目を設定

                    Notification.Show("~b~MeMod~s~: Removed!");
                }
            };
            removeMemos.OnIndexChange += (sender, newindex) => //メニュー項目を移動した場合
            {
                if (rmtarget != null) //削除対象が保存されている場合
                {
                    rmtarget.Description = ""; //削除対象だったデータのDescriptionを削除
                    removeMemos.RefreshIndex(); //表示の更新。ただし一番上にフォーカスが戻ってしまうので、選択位置を固定する場合は以下構文のようにUIMenuItemオブジェクトを選択状態にする必要がある。
                    removeMemos.CurrentSelection = newindex; //移動後の項目位置を選択状態にする

                    rmtarget = null; //削除対象の保存データを削除
                }
            };

            removeMemos.OnMenuChange += (sender, nextmenu, forword) => //メニュー項目を移動した場合
            {
                if (rmtarget != null) //削除対象が保存されている場合
                {
                    rmtarget.Description = ""; //削除対象だったデータのDescriptionを削除
                    removeMemos.RefreshIndex(); //表示の更新。ただし一番上にフォーカスが戻ってしまうので、選択位置を固定する場合は以下構文のようにUIMenuItemオブジェクトを選択状態にする必要がある。
                    rmtarget = null; //削除対象の保存データを削除
                }
            };
        } 
    }
}
