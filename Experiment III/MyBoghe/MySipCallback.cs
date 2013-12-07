using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.doubango.tinyWRAP;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace MyBoghe
{
    public class MySipCallback : SipCallback
    {
        private MainWindow mainWindow;
        public MySipCallback(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public override int OnMessagingEvent(MessagingEvent e)
        {
            tsip_message_event_type_t type = e.getType();
            switch (type)
            {
                case tsip_message_event_type_t.tsip_ao_message:
                    break;
                case tsip_message_event_type_t.tsip_i_message:
                    {
                        SipMessage message = e.getSipMessage();
                        MessagingSession session = e.getSession();
                        uint sessionID;
                        if (session == null)
                        {
                            session = e.takeSessionOwnership();
                        }
                        if (message == null)
                        {
                            session.reject();
                            session.Dispose();
                            return 0;
                        }
                        sessionID = session.getId();
                        String name = message.getSipHeaderValue("f");
                        mainWindow.Dispatcher.BeginInvoke(new Action<string>(mainWindow.SetURI), name);
                        name = name.Substring(4, name.IndexOf('@') - 4);
                        byte[] bytes = message.getSipContent();
                        if (bytes == null || bytes.Length == 0)
                        {
                            session.reject();
                            session.Dispose();
                            return 0;
                        }
                        session.accept();
                        session.Dispose();

                        mainWindow.Dispatcher.BeginInvoke(new Action<string>(mainWindow.addMessage), name + ": " + System.Text.Encoding.Default.GetString(bytes));
                        break;
                    }
            }
            return 0;
        }

        public override int OnRegistrationEvent(RegistrationEvent e)
        {
            tsip_register_event_type_t type = e.getType();
            switch (type)
            {
                case tsip_register_event_type_t.tsip_ao_register: mainWindow.Dispatcher.BeginInvoke(new Action(mainWindow.Register_Success), null); break;
                case tsip_register_event_type_t.tsip_i_newreg: break;
                case tsip_register_event_type_t.tsip_i_register: break;
                case tsip_register_event_type_t.tsip_i_unregister: break;
                case tsip_register_event_type_t.tsip_ao_unregister: mainWindow.Dispatcher.BeginInvoke(new Action(mainWindow.Unregister_Success), null); break;
            }
            return 0;
        }

        public override int OnSubscriptionEvent(SubscriptionEvent e)
        {
            tsip_subscribe_event_type_t type = e.getType();
            switch (type)
            {
                case tsip_subscribe_event_type_t.tsip_i_notify:
                    {
                        try
                        {
                            SubscriptionSession session = e.getSession();
                            SipMessage message = e.getSipMessage();
                            String contentType = message.getSipHeaderValue("c");
                            byte[] content = message.getSipContent();
                            XmlSerializer serializer = new XmlSerializer(typeof(presence));
                            XDocument doc = XDocument.Parse(Encoding.Default.GetString(content));
                            presence pres = serializer.Deserialize(doc.CreateReader()) as presence;
                            Pres_item pres_i = new Pres_item();
                            pres_i.id = pres.entity;
                            pres_i.note = pres.tuple[0].Any[0].InnerText;
                            pres_i.basic = pres.tuple[0].status.basic.ToString();
                            mainWindow.Dispatcher.BeginInvoke(new Action<Pres_item>(mainWindow.ChangeStatus), pres_i);
                        }
                        catch (Exception)
                        {
                        }
                        break;
                    }
                default:
                case tsip_subscribe_event_type_t.tsip_i_subscribe:
                case tsip_subscribe_event_type_t.tsip_ao_subscribe:
                case tsip_subscribe_event_type_t.tsip_i_unsubscribe:
                case tsip_subscribe_event_type_t.tsip_ao_unsubscribe:
                case tsip_subscribe_event_type_t.tsip_ao_notify:
                    {
                        break;
                    }
            }
            return 0;
        }
    }
}
