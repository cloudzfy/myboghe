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
using org.doubango.tinyWRAP;
using System.Timers;
using System.Threading;
namespace Console_Boghe
{
    class Console_Boghe
    {
        private RegistrationSession session;
        private MySipCallback sipCallback;
        private SipStack stack;
        private MessagingSession mSession;
        private String message;
        private String name;

        private String impu;
        private String impi;
        private String pwd;
        private String realm = "sip:open-ims.test";
        private String pcscf_Host = "192.168.16.110";
        private String pcscf_Port = "4060";
        private String call_address;
        private Boolean isCall = false;

        public Console_Boghe()
        {
        }

        public void init()
        {
            System.Console.WriteLine("Please input the user name:");
            name = System.Console.ReadLine();
            impu = "sip:" + name + "@open-ims.test";
            impi = name + "@open-ims.test";
            System.Console.WriteLine("Please input the password:");
            pwd = System.Console.ReadLine();
            SipStack.initialize();
            sipCallback = new MySipCallback();
            stack = new SipStack(sipCallback, realm, impi, impu);
            stack.setPassword(pwd);
            stack.setAMF("0x0000");
            stack.setOperatorId("0x00000000000000000000000000000000");
            stack.setSTUNServer(null, 0);
            stack.setProxyCSCF(pcscf_Host, (ushort)Convert.ToInt32(pcscf_Port), "UDP", "IPv4");
            stack.addHeader("Allow", "INVITE, ACK, CANCEL, BYE, MESSAGE, OPTIONS, NOTIFY, PRACK, UPDATE, REFER");
            stack.addHeader("Privacy", "none");
            stack.addHeader("P-Access-Network-Info", "ADSL;utran-cell-id-3gpp=00000000");
            stack.addHeader("User-Agent", String.Format("IM-client/OMA1.0 Boghe/v{0}", 
                System.Reflection.Assembly.GetEntryAssembly().GetName().Version));
            stack.start();

            session = new RegistrationSession(stack);
            session.setFromUri(impu);
            if (session.register_())
            {
                isCall = false;
            }
            System.Console.WriteLine("Please input who you are going to chat with:");
            name = System.Console.ReadLine();
            call_address = "sip:" + name + "@open-ims.test";

            if (!isCall)
            {
                mSession = new MessagingSession(stack);
                mSession.addHeader("Content-Type", "text/plain");
                mSession.setFromUri(impu);
                mSession.setToUri(call_address);
                isCall = true;
                System.Console.WriteLine("you can talk now.(press q to exit)");
            }
            else
            {
                isCall = false;
            }

            while (true)
            {
                message = System.Console.ReadLine();
                if (message == "q") break;
                else
                {
                    byte[] payload = Encoding.UTF8.GetBytes(message);
                    mSession.send(payload, (uint)payload.Length);
                }
            }
        }
        static void Main(string[] args)
        {
            Console_Boghe consoleBoghe = new Console_Boghe();
            consoleBoghe.init();
        }
    }
}
