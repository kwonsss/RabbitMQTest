﻿using RabbitDevTool.Core;
using System;
using System.Windows;
using System.Windows.Threading;

namespace RabbitDevTool
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {

        private Engine Engine;

        public MainWindow()
        {
            InitializeComponent();

            Engine = new Engine();

            Engine.Output = new Action<string> (DisplayMessage);

            Engine.Initialize();

            Engine.Receiver();
        }
        private void DisplayMessage(string message)
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                this.clientTextBox.AppendText($"{message}\r");
            }));
        }
        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            Engine.Send(this.textMessage.Text);
        }

        private void CallButton_Click(object sender, RoutedEventArgs e)
        {
            Engine.Call();
        }
    }
}
