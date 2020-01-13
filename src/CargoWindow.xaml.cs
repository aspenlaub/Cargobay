using System;
using System.Windows;
using System.Windows.Input;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Application;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Entities;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Jobz;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Access;
using System.Linq;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Components;
using Aspenlaub.Net.GitHub.CSharp.Cargobay.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Pegh.Interfaces;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Application;
using Aspenlaub.Net.GitHub.CSharp.Vishizhukel.Interfaces.Application;
using Autofac;

namespace Aspenlaub.Net.GitHub.CSharp.Cargobay {
    // ReSharper disable once UnusedMember.Global
    public partial class CargoWindow : IJobSelector, ICrypticKeyProvider, IPasswordProvider {
        protected ApplicationCommandController Controller;
        protected CargobayApplication CargobayApplication;
        public Job SelectedJob { get; private set; }
        protected CrypticKey CrypticKey;
        protected IPasswordProvider PasswordProvider;
        protected IContainer Container;

        public CargoWindow() {
            InitializeComponent();
            Container = new ContainerBuilder().UseCargobay().Build();
            Controller = new ApplicationCommandController(ApplicationFeedbackHandler);
            CargobayApplication = new CargobayApplication(Controller, Controller, this, this, this, Container.Resolve<IJobFolderAdjuster>(), Container.Resolve<ISecretRepository>());
            IJobRunner runner = new JobRunner();
            foreach (var job in CargobayApplication.Jobs.Where(job => runner.IsRightMachine(job))) {
                JobTree.Items.Add(job);
            }

            Title = "Cargobay - " + Environment.MachineName;
            SelectedJob = null;
            CommandsEnabledOrDisabledHandler();
            CrypticKey = null;
            PasswordProvider = new PasswordProvider();
        }

        private void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            TextBox.Text = string.Empty;
            TextBoxError.Text = string.Empty;
            SelectedJob = JobTree.SelectedItem as Job;
        }

        private void ButtonPreview_Click(object sender, RoutedEventArgs e) {
            if (Controller == null) {
                return;
            }

            TextBoxError.Text = string.Empty;
            TextBox.Text = string.Empty;
            Cursor = Cursors.Wait;
            Controller.Execute(typeof(PreviewCommand));
        }

        private void ButtonExecute_Click(object sender, RoutedEventArgs e) {
            TextBoxError.Text = string.Empty;
            TextBox.Text = string.Empty;
            Cursor = Cursors.Wait;
            Controller.Execute(typeof(ExecuteCommand));
        }

        public void ApplicationFeedbackHandler(IFeedbackToApplication feedback) {
            switch (feedback.Type) {
                case FeedbackType.CommandExecutionCompleted: {
                    CommandExecutionCompletedHandler();
                }
                break;
                case FeedbackType.CommandsEnabledOrDisabled: {
                    CommandsEnabledOrDisabledHandler();
                }
                break;
                case FeedbackType.LogInformation: {
                    TextBox.Text = TextBox.Text + (TextBox.Text.Length != 0 ? "\r\n" : "") + feedback.Message;
                }
                break;
                case FeedbackType.LogError: {
                    TextBox.Text = TextBox.Text + (TextBox.Text.Length != 0 ? "\r\n" : "") + feedback.Message;
                    TextBoxError.Text = TextBoxError.Text + (TextBoxError.Text.Length != 0 ? "\r\n" : "") + feedback.Message.Trim();
                }
                break;
                case FeedbackType.LogWarning:
                case FeedbackType.CommandIsDisabled: {
                }
                break;
                default: {
                    throw new NotImplementedException();
                }
            }
        }

        private void CommandExecutionCompletedHandler() {
            if (!Controller.IsMainThread()) { return; }

            Cursor = Cursors.Arrow;
        }

        public void CommandsEnabledOrDisabledHandler() {
            var allCommandsEnabled = Controller.Enabled(typeof(PreviewCommand)) && Controller.Enabled(typeof(ExecuteCommand));
            ButtonPreview.IsEnabled = allCommandsEnabled;
            ButtonExecute.IsEnabled = allCommandsEnabled;
        }

        public CrypticKey GetCrypticKey(string clue, string sha1) {
            return GetCrypticKey(clue, sha1, null);
        }

        public CrypticKey GetCrypticKey(string clue, string sha1, ICrypticKeyPrompt crypticKeyPrompt) {
            if (clue != CargoHelper.Clue && sha1 != CargoHelper.Sha1) { return null; }

            if (CrypticKey != null) {
                return CrypticKey;
            }

            ICrypticKeyProvider crypticKeyProvider = new CrypticKeyProvider();
            CrypticKey = crypticKeyProvider.GetCrypticKey(clue, sha1, new CrypticKeyPrompt());
            return CrypticKey;
        }

        public Login GetAccessCodes(string clue) {
            return GetAccessCodes(clue, new AccessCodePrompt());
        }

        public Login GetAccessCodes(string clue, IAccessCodePrompt accessCodePrompt) {
            return PasswordProvider.GetAccessCodes(clue, accessCodePrompt);
        }

        private async void Window_ClosedAsync(object sender, EventArgs e) {
            await Controller.AwaitAllAsynchronousTasks();
            Environment.Exit(1);
        }
    }
}