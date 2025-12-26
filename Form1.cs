using FastColoredTextBoxNS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BrainfxxkCompiler.Compiler;
using BrainfxxkCompiler.Interpreter;

namespace BrainfxxkCompiler {
    public partial class Form1 : Form {
        IBFInterpreter interpreter = new BFInterpreter();
        IBFCompiler compiler = new ILPointerCompiler();
        Thread listUpdater = new Thread(() => { });

        Style GreenStyle = new TextStyle(Brushes.Green, null, FontStyle.Regular);
        Style BlueStyle = new TextStyle(Brushes.Blue, null, FontStyle.Bold);
        Style GrayStyle = new TextStyle(Brushes.Gray, null, FontStyle.Regular);
        Style BlackStyle = new TextStyle(Brushes.Black, null, FontStyle.Bold);

        public Form1() {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            codeBox.DefaultStyle = (TextStyle)BlackStyle;
        }

        private void compileBFButton_Click(object sender, EventArgs e) {
            string path = compilePath.Text + fileName.Text + ".exe";
            compiler.CompileToExe(codeBox.Text, path);
            MessageBox.Show("编译完成");
            Process.Start("explorer.exe", "/select, " + path);
        }

        private void runBFButton_Click(object sender, EventArgs e) {
            resultBox.Text = interpreter.Run(codeBox.Text, out int[] data);
            if (listUpdater.ThreadState == System.Threading.ThreadState.Running) {
                listUpdater.Abort();
            }
            listUpdater = new Thread(() => {
                    if (resultDataBox.Items.Count != interpreter.DataLength) {
                        resultDataBox.Items.Clear();
                        for (int i = 0; i < data.Length; i++) {
                            resultDataBox.Items.Add($"[{i}]" + "\t" + data[i]);
                        }
                        return;
                    }
                    for (int i = 0; i < interpreter.DataLength; i++) {
                        var o = resultDataBox.Items[i];
                        string oS = o.ToString().Split(new[] { ']' }, StringSplitOptions.RemoveEmptyEntries)[1];
                        if (int.Parse(oS) != data[i]) {
                            resultDataBox.Items[i] = ($"[{i}]" + "\t" + data[i]);
                        }
                    }
                }
            );
            listUpdater.Start();
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e) { }

        private void fileName_TextChanged(object sender, EventArgs e) {
            if (fileName.TextLength < 1) {
                fileName.Text = "BF";
            }
        }

        private void codeBox_TextChanged(object sender, TextChangedEventArgs e) {
            e.ChangedRange.ClearStyle(GreenStyle);
            //注释
            e.ChangedRange.SetStyle(GreenStyle, @"//.*$", RegexOptions.Multiline);

            //括号高亮
            e.ChangedRange.SetStyle(BlueStyle, @"[\[\]]", RegexOptions.Multiline);

            //非特定字符灰色
            e.ChangedRange.SetStyle(GrayStyle, @"[^\>\<\+\-\.\,\[\]]", RegexOptions.Multiline);
        }
    }
}