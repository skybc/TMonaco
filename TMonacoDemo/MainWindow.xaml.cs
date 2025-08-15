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

namespace TMonacoDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            editor.MonacoIsLoadedChanged += Editor_MonacoIsLoadedChanged;
        }

        private async void Editor_MonacoIsLoadedChanged(object? sender, EventArgs e)
        {
            await editor.SetLanguageAsync(TMonaco.MonacoLanguage.Csharp);
            // SetCustomSuggestAsync,
            // 获取已经加载的全部程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            await editor.SetCustomSuggestAsync(assemblies);

        }
    }
}