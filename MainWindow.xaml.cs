
using System;
using System.Windows;

namespace WpfFluentApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SetAtivarButtonState()
        {
            
            AtivarButton.IsEnabled = StatusTextBox.Text == "Desativada";
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {

            string computer = HostnameTextBox.Text.Trim();
            SenhaTextBox.Text = string.Empty;
            ExpiraTextBox.Text = string.Empty;
            StatusTextBox.Text = string.Empty;
            UltimaModTextBox.Text = string.Empty;
            SetLoadingFooter(true);
            AtivarButton.IsEnabled = false;

            if (string.IsNullOrWhiteSpace(computer))
            {
                SenhaTextBox.Text = "Forneça um hostname.";
                SetLoadingFooter(false);
                return;
            }

            try
            {
                string ldapPath = "";
                var result = await System.Threading.Tasks.Task.Run(() =>
                {
                    using (var root = new System.DirectoryServices.DirectoryEntry(ldapPath))
                    using (var searcher = new System.DirectoryServices.DirectorySearcher(root))
                    {
                        searcher.Filter = $"(sAMAccountName={computer}$)";
                        searcher.PropertiesToLoad.Add("ms-Mcs-AdmPwd");
                        searcher.PropertiesToLoad.Add("ms-Mcs-AdmPwdExpirationTime");
                        searcher.PropertiesToLoad.Add("userAccountControl");
                        searcher.PropertiesToLoad.Add("whenChanged");
                        return searcher.FindOne();
                    }
                });

                if (result != null)
                {
                    
                    if (result.Properties["ms-Mcs-AdmPwd"].Count > 0)
                    {
                        SenhaTextBox.Text = result.Properties["ms-Mcs-AdmPwd"][0]?.ToString();
                    }
                    else
                    {
                        SenhaTextBox.Text = "Senha não encontrada no LAPS.";
                    }

                    
                    if (result.Properties["ms-Mcs-AdmPwdExpirationTime"].Count > 0)
                    {
                        var fileTime = (long)result.Properties["ms-Mcs-AdmPwdExpirationTime"][0];
                        var expires = DateTime.FromFileTimeUtc(fileTime).ToLocalTime();
                        ExpiraTextBox.Text = expires.ToString();
                    }
                    else
                    {
                        ExpiraTextBox.Text = "N/A";
                    }

                    
                    if (result.Properties["userAccountControl"].Count > 0)
                    {
                        int uac = Convert.ToInt32(result.Properties["userAccountControl"][0]);
                        
                        bool disabled = (uac & 0x2) != 0;
                        StatusTextBox.Text = disabled ? "Desativada" : "Ativada";
                        StatusTextBox.Foreground = disabled
                            ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red)
                            : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                        SetAtivarButtonState();
                    }
                    else
                    {
                        StatusTextBox.Text = "N/A";
                        SetAtivarButtonState();
                    }

                    
                    if (result.Properties["whenChanged"].Count > 0)
                    {
                        if (DateTime.TryParse(result.Properties["whenChanged"][0]?.ToString(), out var changed))
                        {
                            UltimaModTextBox.Text = changed.ToString();
                        }
                        else
                        {
                            UltimaModTextBox.Text = result.Properties["whenChanged"][0]?.ToString();
                        }
                    }
                    else
                    {
                        UltimaModTextBox.Text = "N/A";
                    }
                }
                else
                {
                    SenhaTextBox.Text = "Senha não encontrada no LAPS.";
                    ExpiraTextBox.Text = string.Empty;
                    StatusTextBox.Text = string.Empty;
                    SetAtivarButtonState();
                    UltimaModTextBox.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                SenhaTextBox.Text = $"{ex.Message}";
            }
            finally
            {
                SetLoadingFooter(false);
            }
        }

        private void SetLoadingFooter(bool isLoading)
        {
            if (this.FindName("FooterLoadingTextBlock") is System.Windows.Controls.TextBlock footer)
            {
                footer.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
                if (isLoading)
                    footer.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x77, 0x77, 0x77));
            }
        }

        private async void AtivarButton_Click(object sender, RoutedEventArgs e)
        {
            SetLoadingFooter(true);
            if (this.FindName("FooterLoadingTextBlock") is System.Windows.Controls.TextBlock footer)
            {
                footer.Text = "carregando...";
                footer.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x77, 0x77, 0x77));
            }
            try
            {
                string computer = HostnameTextBox.Text.Trim();
                string ldapPath = "LDAP://azure-dc01.grupoaec.com.br/DC=grupoaec,DC=com,DC=br";
                await System.Threading.Tasks.Task.Run(() =>
                {
                    using (var root = new System.DirectoryServices.DirectoryEntry(ldapPath))
                    using (var searcher = new System.DirectoryServices.DirectorySearcher(root))
                    {
                        searcher.Filter = $"(sAMAccountName={computer}$)";
                        var result = searcher.FindOne();
                        if (result != null)
                        {
                            using (var entry = result.GetDirectoryEntry())
                            {
                    
                                if (entry.Properties["userAccountControl"].Value is int uac)
                                {
                                    entry.Properties["userAccountControl"].Value = uac & ~0x2;
                                    entry.CommitChanges();
                                }
                            }
                        }
                    }
                });
                
                await System.Threading.Tasks.Task.Delay(500);
                SearchButton_Click(null, null);
            }
            catch (System.UnauthorizedAccessException)
            {
                if (this.FindName("FooterLoadingTextBlock") is System.Windows.Controls.TextBlock footerPerm)
                {
                    footerPerm.Text = "erro de permissão";
                    footerPerm.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    footerPerm.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                if (this.FindName("FooterLoadingTextBlock") is System.Windows.Controls.TextBlock footerEx)
                {
                    footerEx.Text = ex.Message;
                    footerEx.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                    footerEx.Visibility = Visibility.Visible;
                }
            }
            finally
            {
                SetLoadingFooter(false);
            }
        }
    }
}
