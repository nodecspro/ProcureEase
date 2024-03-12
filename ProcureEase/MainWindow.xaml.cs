using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ProcureEase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Вывод сообщения об успешной авторизации
            MessageBox.Show("Вы успешно авторизовались!", "Авторизация", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}