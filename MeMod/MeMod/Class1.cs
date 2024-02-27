using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using GTA;
using GTA.UI;
using NativeUI;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using System.IO;
using System.Reflection;
using System.Drawing;

namespace MeMod
{
    public class Class1 : Script
    {
        //iniファイルの読み込みに必要な処理。Pythonのimport文、C#のUsing文の派生版みたいなもんよ
        [DllImport("kernel32.dll", EntryPoint = "GetPrivateProfileStringW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
        //こちらはiniファイルの書き込み。
        [DllImport("kernel32.dll", EntryPoint = "WritePrivateProfileStringW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool WritePrivateProfileString(string lpAppName, string lpKeyName, string lpString, string lpFileName);


        static TextElement te;
        public static ScriptSettings ini = ScriptSettings.Load(@"scripts\MeMod.ini");
        private static Keys menukey; //キー設定を用意

        public Class1()
        {
            Tick += onTick;
            KeyDown += onKeyDown;
            // Interval = 1000;


            /// iniのデータを読み込む (セクション、キー、デフォルト値)
            menukey = ini.GetValue<Keys>("Keys", "MenuKey", Keys.F11);
            int c = Menu.CreateMenu();
            Notification.Show($"~b~MeMod~s~: ~g~{c}~s~ memos loaded!");
        }

        private void onKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == menukey && !Menu.modMenuPool.IsAnyMenuOpen()) //iniで指定されたキーと押されたキーが一致、メニューが非表示状態である場合
            {
                Menu.show();
            }
        }

        /*
        bool PullOver = false;
        bool prev_PullOver = false;
        bool Err_Notify = true;
        */

        private void onTick(object sender, EventArgs e)
        {
            if (Menu.modMenuPool != null) //これがないとVisibleがTrueでもメニューが表示されないので注意。
                Menu.modMenuPool.ProcessMenus();

            if (Menu.showing.Checked && te != null)
            {
                te.ScaledDraw();
            }

            #region LSPDFR_Link
            /*
            try
            {
                PullOver = MeMod_Handler.Main.IsPullover();
                if (PullOver)
                {
                    prev_PullOver = true;
                }
                else //職務質問終了時
                {
                    if (prev_PullOver && reset) //さっきまで職務質問だったとき、AutoReset有効時
                    {
                        prev_PullOver = false;

                        XElement xml = XElement.Load(@"scripts\MeMod\memos.xml");
                        //タグ内の情報を取得する
                        IEnumerable<XElement> infos = from item in xml.Elements("memos").Elements("memo")
                                                      select item;

                        //memos→memo分ループして、デフォルト設定に戻す
                        foreach (XElement info in infos)
                        {
                            Menu.MemoState[info.Element("memo").Value] = Menu.TorF((string)info.Attribute("default"));
                        }

                    }
                }
                if(!Err_Notify)
                {
                    Notification.Show($"~b~MeMod~s~: Memod Handler was ~g~successfully loaded!");
                    Err_Notify = true; //次回以降エラー通知の有効
                }
            }
            catch (FileNotFoundException ex)
            {
                if (Err_Notify)
                {
                    Notification.Show($"~b~MeMod~s~: ~o~Failed to load Memod Handler.~n~Some functions will be limited.");
                    Err_Notify = false; //これ以降通知しない
                }
            }
            */

            #endregion
        }
        public static void setText(string content, bool show_left)
        {
            PointF p;
            if (show_left)
            {
                p = new PointF(10, 240); // PointFはX,Yの座標。
                te = new TextElement(content, p, 0.5f); //末尾にFをつけてFloatをキャスト(明示的変換）。
                te.Alignment = Alignment.Left; //左揃え
            }
            else
            {
                p = new PointF(1270, 240);
                te = new TextElement(content, p, 0.5f); //末尾にFをつけてFloatをキャスト(明示的変換）。
                te.Alignment = Alignment.Right; //右揃え
            }

            te.Outline= true; //縁取り
            te.Shadow = true; //影付き文字
        }

        public static void remText()
        {
            te = null;
        }

    }
}
