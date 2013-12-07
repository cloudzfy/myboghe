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
            Pres_impu_text.IsEnabled = false;
            Add_button.IsEnabled = false;
            Pres_listView.IsEnabled = false;
            Mystatus_text.IsEnabled = false;
            Add_button.IsEnabled = false;
            Remove_button.IsEnabled = false;

            Mystatus_text.Items.Add("Online");
            Mystatus_text.Items.Add("Busy");
            Mystatus_text.Items.Add("Be Right Back");
            Mystatus_text.Items.Add("Away");
            Mystatus_text.Items.Add("On The Phone");
            Mystatus_text.Items.Add("Hyper Available");
            Mystatus_text.Items.Add("Offline");

        }

        private void Open_OnClick(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Visible;
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
            Pres_impu_text.IsEnabled = true;
            Add_button.IsEnabled = true;
            Pres_listView.IsEnabled = true;
            Mystatus_text.IsEnabled = true;
            Add_button.IsEnabled = true;
            Remove_button.IsEnabled = true;
            SigninMenu.IsEnabled = false;
            SignoutMenu.IsEnabled = true;

            Impu_text.IsEnabled = false;
            Impi_text.IsEnabled = false;
            Pwd_text.IsEnabled = false;
            Realm_text.IsEnabled = false;
            Pcscfhost_text.IsEnabled = false;
            Pcscfport_text.IsEnabled = false;

            SubscriptionSession subcriptionSession_reg = new SubscriptionSession(stack);
            subcriptionSession_reg.addHeader("Event", "reg");
            subcriptionSession_reg.addHeader("Accept", "application/reginfo+xml");
            subcriptionSession_reg.addHeader("Allow-Events", "refer, presence, presence.winfo, xcap-diff, conference");
            subcriptionSession_reg.setFromUri(Impu_text.Text);
            subcriptionSession_reg.setToUri(Impu_text.Text);
            subcriptionSession_reg.subscribe();

            PublicationSession publicationSession = new PublicationSession(stack);
            publicationSession.setFromUri(Impu_text.Text);
            publicationSession.setToUri(Impu_text.Text);
            publicationSession.addHeader("Event", "presence");
            publicationSession.addHeader("Content-Type", "application/pidf+xml");

            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces(
            new XmlQualifiedName[]{
                new XmlQualifiedName(String.Empty, "urn:ietf:params:xml:ns:pidf"),
                new XmlQualifiedName("op", "urn:oma:xml:prs:pidf:oma-pres"),
                new XmlQualifiedName("pdm", "urn:ietf:params:xml:ns:pidf:data-model"),
                new XmlQualifiedName("rpid", "urn:ietf:params:xml:ns:pidf:rpid"),
                new XmlQualifiedName("caps", "urn:ietf:params:xml:ns:pidf:caps"),
                new XmlQualifiedName("cp", "urn:ietf:params:xml:ns:pidf:cipid"),
                new XmlQualifiedName("p", "urn:ietf:params:xml:ns:pidf-diff")
            });

            presence pres = new presence();
            pres.entity = Impu_text.Text;
            pres.tuple = new tuple[1];
            pres.tuple[0] = new tuple();
            pres.tuple[0].status = new status();
            pres.tuple[0].status.basic = basic.open;
            pres.tuple[0].status.basicSpecified = true;
            pres.tuple[0].note = new note[1];
            pres.tuple[0].note[0] = new note();
            pres.tuple[0].note[0].Value = Mystatus_text.Text;
            pres.tuple[0].timestamp = DateTime.Now;

            byte[] content;
            using (MemoryStream stream = new MemoryStream())
            {
                XmlWriterSettings xmlSettings = new XmlWriterSettings();
                xmlSettings.Encoding = new UTF8Encoding(false);
                xmlSettings.OmitXmlDeclaration = false;
                xmlSettings.Indent = true;

                using (XmlWriter xmlWriter = XmlWriter.Create(stream, xmlSettings))
                {
                    XmlSerializer serializer = new XmlSerializer(pres.GetType());
                    serializer.Serialize(xmlWriter, pres, namespaces);
                }
                content = stream.ToArray();
            }

            publicationSession.publish(content, (uint)content.Length);

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
            Pres_impu_text.IsEnabled = false;
            Add_button.IsEnabled = false;
            Pres_listView.IsEnabled = false;
            Mystatus_text.IsEnabled = false;
            Add_button.IsEnabled = false;
            Remove_button.IsEnabled = false;
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

        private void Add_button_Click(object sender, RoutedEventArgs e)
        {
            SubscriptionSession subcriptionSession_pres = new SubscriptionSession(stack);
            subcriptionSession_pres.addHeader("Event", "presence");
            subcriptionSession_pres.addHeader("Accept", "multipart/related, application/pidf+xml, application/rlmi+xml, application/rpid+xml");
            subcriptionSession_pres.addHeader("Allow-Events", "refer, presence, presence.winfo, xcap-diff, conference");
            subcriptionSession_pres.setFromUri(Impu_text.Text);
            subcriptionSession_pres.setToUri(Pres_impu_text.Text);
            subcriptionSession_pres.subscribe();
            bool isAdd = false;
            for (int i = 0; i < Pres_listView.Items.Count; i++)
            {
                if (Pres_impu_text.Text == ((Pres_item)Pres_listView.Items[i]).id)
                {
                    isAdd = true;
                }
            }
            if (!isAdd)
            {
                Pres_item pres = new Pres_item();
                pres.id = Pres_impu_text.Text;
                pres.basic = "closed";
                pres.note = "";
                Pres_listView.Items.Add(pres);
            }
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

        public void ChangeStatus(Pres_item pres)
        {
            bool isAdd = false;
            for(int i=0;i<Pres_listView.Items.Count;i++)
            {
                if(((Pres_item)Pres_listView.Items[i]).id==pres.id)
                {
                    isAdd = true;
                    Pres_listView.Items[i] = pres;
                    break;
                }
            }
            if (!isAdd)
            {
                Pres_listView.Items.Add(pres);
            }
        }

        private void Remove_button_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < Pres_listView.Items.Count; i++)
            {
                if (((Pres_item)Pres_listView.Items[i]).id == Pres_impu_text.Text)
                {
                    Pres_listView.Items.Remove(Pres_listView.Items[i]);
                    SubscriptionSession subcriptionSession_pres = new SubscriptionSession(stack);
                    subcriptionSession_pres.addHeader("Event", "presence");
                    subcriptionSession_pres.addHeader("Accept", "multipart/related, application/pidf+xml, application/rlmi+xml, application/rpid+xml");
                    subcriptionSession_pres.addHeader("Allow-Events", "refer, presence, presence.winfo, xcap-diff, conference");
                    subcriptionSession_pres.setFromUri(Impu_text.Text);
                    subcriptionSession_pres.setToUri(Pres_impu_text.Text);
                    subcriptionSession_pres.unSubscribe();
                }
            }
        }

      


        private void Mystatus_text_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PublicationSession publicationSession = new PublicationSession(stack);
            publicationSession.setFromUri(Impu_text.Text);
            publicationSession.setToUri(Impu_text.Text);
            publicationSession.addHeader("Event", "presence");
            publicationSession.addHeader("Content-Type", "application/pidf+xml");

            XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces(
            new XmlQualifiedName[]{
                new XmlQualifiedName(String.Empty, "urn:ietf:params:xml:ns:pidf"),
                new XmlQualifiedName("op", "urn:oma:xml:prs:pidf:oma-pres"),
                new XmlQualifiedName("pdm", "urn:ietf:params:xml:ns:pidf:data-model"),
                new XmlQualifiedName("rpid", "urn:ietf:params:xml:ns:pidf:rpid"),
                new XmlQualifiedName("caps", "urn:ietf:params:xml:ns:pidf:caps"),
                new XmlQualifiedName("cp", "urn:ietf:params:xml:ns:pidf:cipid"),
                new XmlQualifiedName("p", "urn:ietf:params:xml:ns:pidf-diff")
            });

            presence pres = new presence();
            pres.entity = Impu_text.Text;
            pres.tuple = new tuple[1];
            pres.tuple[0] = new tuple();
            pres.tuple[0].status = new status();
            pres.tuple[0].status.basic = basic.open;
            pres.tuple[0].status.basicSpecified = true;
            pres.tuple[0].note = new note[1];
            pres.tuple[0].note[0] = new note();
            pres.tuple[0].note[0].Value = Mystatus_text.Text;
            pres.tuple[0].timestamp = DateTime.Now;

            byte[] content;
            using (MemoryStream stream = new MemoryStream())
            {
                XmlWriterSettings xmlSettings = new XmlWriterSettings();
                xmlSettings.Encoding = new UTF8Encoding(false);
                xmlSettings.OmitXmlDeclaration = false;
                xmlSettings.Indent = true;

                using (XmlWriter xmlWriter = XmlWriter.Create(stream, xmlSettings))
                {
                    XmlSerializer serializer = new XmlSerializer(pres.GetType());
                    serializer.Serialize(xmlWriter, pres, namespaces);
                }
                content = stream.ToArray();
            }

            publicationSession.publish(content, (uint)content.Length);
        }

        private void Mystatus_text_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PublicationSession publicationSession = new PublicationSession(stack);
                publicationSession.setFromUri(Impu_text.Text);
                publicationSession.setToUri(Impu_text.Text);
                publicationSession.addHeader("Event", "presence");
                publicationSession.addHeader("Content-Type", "application/pidf+xml");

                XmlSerializerNamespaces namespaces = new XmlSerializerNamespaces(
                new XmlQualifiedName[]{
                new XmlQualifiedName(String.Empty, "urn:ietf:params:xml:ns:pidf"),
                new XmlQualifiedName("op", "urn:oma:xml:prs:pidf:oma-pres"),
                new XmlQualifiedName("pdm", "urn:ietf:params:xml:ns:pidf:data-model"),
                new XmlQualifiedName("rpid", "urn:ietf:params:xml:ns:pidf:rpid"),
                new XmlQualifiedName("caps", "urn:ietf:params:xml:ns:pidf:caps"),
                new XmlQualifiedName("cp", "urn:ietf:params:xml:ns:pidf:cipid"),
                new XmlQualifiedName("p", "urn:ietf:params:xml:ns:pidf-diff")
            });

                presence pres = new presence();
                pres.entity = Impu_text.Text;
                pres.tuple = new tuple[1];
                pres.tuple[0] = new tuple();
                pres.tuple[0].status = new status();
                pres.tuple[0].status.basic = basic.open;
                pres.tuple[0].status.basicSpecified = true;
                pres.tuple[0].note = new note[1];
                pres.tuple[0].note[0] = new note();
                pres.tuple[0].note[0].Value = Mystatus_text.Text;
                pres.tuple[0].timestamp = DateTime.Now;

                byte[] content;
                using (MemoryStream stream = new MemoryStream())
                {
                    XmlWriterSettings xmlSettings = new XmlWriterSettings();
                    xmlSettings.Encoding = new UTF8Encoding(false);
                    xmlSettings.OmitXmlDeclaration = false;
                    xmlSettings.Indent = true;

                    using (XmlWriter xmlWriter = XmlWriter.Create(stream, xmlSettings))
                    {
                        XmlSerializer serializer = new XmlSerializer(pres.GetType());
                        serializer.Serialize(xmlWriter, pres, namespaces);
                    }
                    content = stream.ToArray();
                }

                publicationSession.publish(content, (uint)content.Length);
            }
        }
    }
}