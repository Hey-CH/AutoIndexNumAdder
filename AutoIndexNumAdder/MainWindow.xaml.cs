using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Globalization;

namespace AutoIndexNumAdder {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        ViewModel vm;
        public MainWindow() {
            InitializeComponent();
            vm = new ViewModel();
            this.DataContext = vm;
        }

        private void listBox1_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void listBox1_Drop(object sender, DragEventArgs e) {
            var showMsg = false;//警告メッセージを出したかどうか
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                foreach (var path in (string[])e.Data.GetData(DataFormats.FileDrop)) {
                    if (!File.Exists(path)) continue;//ファイル以外は無視
                    if (vm.Files.Count > 0) {
                        //同じDirectoryのファイルしか扱わない
                        var dir = new FileInfo(vm.Files[0].Path).Directory.FullName;
                        var tmp = new FileInfo(path).Directory.FullName;
                        if (dir.ToLower() != tmp.ToLower()) {
                            if (!showMsg) {
                                MessageBox.Show("追加したファイルと違うフォルダのファイルは追加できません。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                showMsg = true;
                            }
                            continue;
                        }
                    }
                    //同じファイルは無視して追加
                    if (!vm.Files.Select(f => f.Path.ToLower()).Contains(path.ToLower())) vm.Files.Add(new FileInformation(path));
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            var sindex = Regex.Replace(vm.StartIndex, "[^0-9]+", "");
            if (long.TryParse(sindex, out long val)) vm.StartIndex = val.ToString();
            else vm.StartIndex = "1";
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            if (vm.SelectedFile != null) vm.Files.Remove(vm.SelectedFile);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            vm.Files.Clear();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            AddIndexNum();
        }

        //Enterキーでも実行[2021/07/24]
        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && vm.Files.Count > 0) {
                AddIndexNum();
            }
        }

        private void AddIndexNum() {
            //ファイルをセットしないと実行しないようにしたのでコメント[2021/07/24]
            //if (vm.Files.Count <= 0) {
            //    MessageBox.Show("ファイルをドラッグ＆ドロップしてください。");
            //    return;
            //}

            var dir = new FileInfo(vm.Files[0].Path).Directory.FullName;
            var ix = GetTrueStartIndex(dir, vm.Prefix);
            var max = ix + vm.Files.Count - 1;
            var fmt = "{0:D" + max.ToString().Length + "}";

            //既存のファイルの桁揃え（YesNoしても良いけど個人的に強制で。）
            var r = new Regex($"^{vm.Prefix}(\\d+)\\.[^.]+$");
            var exists = Directory.GetFiles(dir)
                .Where(f => r.IsMatch(new FileInfo(f).Name)).ToList();
            foreach (var f in exists.Except(vm.Files.Select(f => f.Path))) {//変更するファイルは桁ぞろえしない
                var fi = new FileInfo(f);
                var fn = vm.Prefix + string.Format(fmt, GetFileNumber(f, vm.Prefix)) + fi.Extension;
                if (fi.Name.ToLower() != fn.ToLower()) fi.MoveTo(System.IO.Path.Combine(fi.DirectoryName, fn));
            }

            //指定されたファイル名の変更
            foreach (var f in vm.Files.Select(f => f.Path)) {
                var fi = new FileInfo(f);
                var fn = vm.Prefix + string.Format(fmt, ix++) + fi.Extension;
                fi.MoveTo(System.IO.Path.Combine(fi.DirectoryName, fn));
            }

            //クリア
            vm.Files.Clear();
        }
        /// <summary>
        /// dir内のprefix+番号のファイルを取得し、その番号の最大値+1を返します
        /// </summary>
        /// <param name="dir">フォルダ</param>
        /// <param name="prefix">接頭辞</param>
        /// <returns>重複しないような最初のIndex値</returns>
        private long GetTrueStartIndex(string dir, string prefix) {
            var r = new Regex($"^{prefix}(\\d+)\\.[^.]+$");
            //prefixで始まるファイルが無い場合を考えていなかったので修正[2021/07/24]
            var tmp = Directory.GetFiles(dir).Where(f => r.IsMatch(new FileInfo(f).Name));
            var fidx = tmp.Count() > 0 ? tmp.Select(f => GetFileNumber(f, vm.Prefix)).Max() + 1 : 0;//prefixが付いたファイルの最大値+1 or ファイルが無い場合0
            var iidx = long.Parse(vm.StartIndex);//入力されている数値
            //存在するファイルのIndexより入力値の方が大きい場合そちらを設定する
            return fidx < iidx ? iidx : fidx;
        }
        /// <summary>
        /// 指定されたファイルの接頭辞＋番号で表される番号を返します
        /// </summary>
        /// <param name="path">ファイルパス</param>
        /// <param name="prefix">接頭辞</param>
        /// <returns>ファイルの番号</returns>
        private long GetFileNumber(string path, string prefix) {
            var r = new Regex($"^{prefix}(\\d+)\\.[^.]+$");
            if (!r.IsMatch(new FileInfo(path).Name)) return -1;
            return long.Parse(r.Match(new FileInfo(path).Name).Groups[1].Value.TrimStart('0'));
        }
    }
    public class ViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        string _Prefix = "_";
        public string Prefix {
            get { return _Prefix; }
            set {
                _Prefix = value;
                OnPropertyChanged(nameof(Prefix));
            }
        }
        string _StartIndex = "1";
        public string StartIndex {
            get { return _StartIndex; }
            set {
                _StartIndex = value;
                OnPropertyChanged(nameof(StartIndex));
            }
        }
        public ObservableCollection<FileInformation> Files { get; set; } = new ObservableCollection<FileInformation>();
        FileInformation _SelectedFile = null;
        public FileInformation SelectedFile {
            get { return _SelectedFile; }
            set {
                _SelectedFile = value;
                OnPropertyChanged(nameof(SelectedFile));
            }
        }
        public bool Executable {
            get { return Files.Count > 0; }
        }
    }
    /// <summary>
    /// ファイルの情報を格納するクラス
    /// </summary>
    public class FileInformation {
        public string Path { get; set; }
        public string Name { get { return new FileInfo(Path).Name; } }
        public int IndexNumber { get; set; }

        public FileInformation(string path) {
            Path = path;
        }
        public override string ToString() {
            return Name;
        }
    }
    /// <summary>
    /// 0じゃないintをTrueで返すコンバーター
    /// </summary>
    public class NotZeroIntToTrueConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return ((int)value) > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            var tmp = (bool)value;
            return tmp ? 1 : 0;
        }
    }
}
