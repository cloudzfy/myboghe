using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using org.doubango.tinyWRAP;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Console_Boghe
{
    public class MySipCallback : SipCallback
    {
        public MySipCallback()
        {
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
                        Console.WriteLine(name + ": " + System.Text.Encoding.Default.GetString(bytes));
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
                case tsip_register_event_type_t.tsip_ao_register: System.Console.WriteLine("(Registed success)"); break;
                case tsip_register_event_type_t.tsip_i_newreg: break;
                case tsip_register_event_type_t.tsip_i_register: break;
                case tsip_register_event_type_t.tsip_i_unregister: break;
                case tsip_register_event_type_t.tsip_ao_unregister:  break;
            }
            return 0;
        }
    }
}
