/*
 * myboghe, A PC-based IMS UE for Teaching Experiments
 * Copyright (C) 2013, Cloudzfy
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
 
using System;
using System.Collections.Generic;
using System.Linq;
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
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Threading;
using System.IO;
using org.doubango.tinyWRAP;

namespace MyBoghe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private RegistrationSession session;
        private MySipCallback sipCallback;
        private SipStack stack;

        public MainWindow()
        {
            InitializeComponent();
            
            call_address.IsEnabled = false;
            messageList.IsEnabled = false;
            inputText.IsEnabled = false;
            Send_button.IsEnabled = false;
            SignoutMenu.IsEnabled = false;

        }

        private void Exit_OnClick(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }

        private void Signin_Click(object sender, RoutedEventArgs e)
        {
            SipStack.initialize();
            sipCallback = new MySipCallback(this);
            stack = new SipStack(sipCallback, Realm_text.Text, Impi_text.Text, Impu_text.Text);
            stack.setPassword(Pwd_text.Password);
            stack.setAMF("0x0000");
            stack.setOperatorId("0x00000000000000000000000000000000");
            stack.setSTUNServer(null, 0);

            stack.setProxyCSCF(Pcscfhost_text.Text, (ushort)Convert.ToInt32(Pcscfport_text.Text), "UDP", "IPv4");
            stack.addHeader("Allow", "INVITE, ACK, CANCEL, BYE, MESSAGE, OPTIONS, NOTIFY, PRACK, UPDATE, REFER");
            stack.addHeader("Privacy", "none");
            stack.addHeader("P-Access-Network-Info", "ADSL;utran-cell-id-3gpp=00000000");
            stack.addHeader("User-Agent", String.Format("IM-client/OMA1.0 Boghe/v{0}", System.Reflection.Assembly.GetEntryAssembly().GetName().Version));
            stack.start();

            session = new RegistrationSession(stack);
            session.setFromUri(Impu_text.Text);
            session.register_();
        }

        public void Register_Success()
        {
            call_address.IsEnabled = true;
            messageList.IsEnabled = true;
            inputText.IsEnabled = true;
            Send_button.IsEnabled = true;
            SigninMenu.IsEnabled = false;
            SignoutMenu.IsEnabled = true;

            Impu_text.IsEnabled = false;
            Impi_text.IsEnabled = false;
            Pwd_text.IsEnabled = false;
            Realm_text.IsEnabled = false;
            Pcscfhost_text.IsEnabled = false;
            Pcscfport_text.IsEnabled = false;

        }

        private void inputText_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                MessagingSession mSession = new MessagingSession(stack);
                mSession.addHeader("Content-Type", "text/plain");
                mSession.setFromUri(Impu_text.Text);
                mSession.setToUri(call_address.Text);
                byte[] payload = Encoding.UTF8.GetBytes(inputText.Text);
                mSession.send(payload, (uint)payload.Length);
                messageList.Text = messageList.Text + "I: " + inputText.Text + "\n";
                inputText.Text = "";
            }
        }

        public void addMessage(string message)
        {
            messageList.Text = messageList.Text + message + "\n";
        }

        private void Signout_Click(object sender, RoutedEventArgs e)
        {
            session.unRegister();
        }

        public void Unregister_Success()
        {
            call_address.IsEnabled = false;
            messageList.IsEnabled = false;
            inputText.IsEnabled = false;
            Send_button.IsEnabled = false;
            SigninMenu.IsEnabled = true;
            SignoutMenu.IsEnabled = false;

            Impu_text.IsEnabled = true;
            Impi_text.IsEnabled = true;
            Pwd_text.IsEnabled = true;
            Realm_text.IsEnabled = true;
            Pcscfhost_text.IsEnabled = true;
            Pcscfport_text.IsEnabled = true;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        private void Send_button_Click(object sender, RoutedEventArgs e)
        {
            MessagingSession mSession = new MessagingSession(stack);
            mSession.addHeader("Content-Type", "text/plain");
            mSession.setFromUri(Impu_text.Text);
            mSession.setToUri(call_address.Text);
            byte[] payload = Encoding.UTF8.GetBytes(inputText.Text);
            mSession.send(payload, (uint)payload.Length);
            messageList.Text = messageList.Text + "I: " + inputText.Text + "\n";
            inputText.Text = "";
        }

        public void SetURI(string uri)
        {
            call_address.Text = uri;
        }

    }
}