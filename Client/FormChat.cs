using Model;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocket4Net;

namespace Client
{
    public partial class FormChat : Form
    {
        WebSocket websocket = new WebSocket("ws://localhost:8181/");
        String userName = string.Empty;
        Guid token = Guid.Empty;
        bool isLoggedIn = false;
        List<StatusEntity> statuses = new List<StatusEntity>();

        public FormChat()
        {
            InitializeComponent();
            websocket = new WebSocket("ws://localhost:8181/");
            websocket.Opened += websocket_Opened;
            websocket.Error += websocket_Error;
            websocket.Closed += websocket_Closed;
            websocket.MessageReceived += websocket_MessageReceived;
        }

        void websocket_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            try
            {
                var entity = JsonConvert.DeserializeObject<ChatEntity>(e.Message);
                if (entity.MessageType == MessageType.Message)
                {
                    List<StatusEntity> newEntities = new List<StatusEntity>();
                    foreach (StatusEntity stat in statuses)
                    {
                        if (stat.UserName != entity.UserName)
                        {
                            newEntities.Add(stat);
                        }
                    }
                    statuses = newEntities;

                    AppendText(txtMessage, entity.UserName + " : " + entity.Message + "\r\n");
                }
                else if (entity.MessageType == MessageType.Login)
                {
                    if (!isLoggedIn && entity.Token != Guid.Empty)
                    {
                        SetControlEnabled(btnConnect, false);
                        SetControlEnabled(btnDisconnect, true);
                        SetControlEnabled(txtName, false);
                        SetControlEnabled(txtSend, true);
                        SetControlFocus(txtSend);
                        userName = txtName.Text;
                        token = entity.Token;
                        isLoggedIn = true;
                    }
                    else
                    {
                        if(entity.UserName == userName && entity.Token != token)
                        {
                            try
                            {
                                if (websocket.State == WebSocketState.Open)
                                    websocket.Close();
                            }
                            catch
                            {
                                return;
                            }
                        }
                    }
                }
                else if (entity.MessageType == MessageType.Logout)
                {
                    try
                    {
                        if (isLoggedIn && websocket.State == WebSocketState.Open)
                        {
                            websocket.Close();
                        }
                    }
                    catch
                    {
                        return;
                    }
                }
                else
                {
                    if (entity.UserName != userName)
                    {
                        StatusEntity model = new StatusEntity();
                        model.UserName = entity.UserName;
                        model.Time = 2000;
                        List<StatusEntity> newEntities = new List<StatusEntity>();
                        foreach (StatusEntity stat in statuses)
                        {
                            if (stat.UserName != entity.UserName)
                            {
                                newEntities.Add(stat);
                            }
                        }
                        statuses = newEntities;

                        statuses.Add(model);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        void websocket_Closed(object sender, EventArgs e)
        {
            //MessageBox.Show("Bye, " + txtName.Text);

            SetControlEnabled(btnConnect, true);
            SetControlEnabled(btnDisconnect, false);
            SetControlEnabled(txtName, true);
            SetTextBoxText(txtSend, string.Empty);
            SetControlEnabled(txtSend, false);
            userName = string.Empty;
            token = Guid.Empty;
            isLoggedIn = false;
        }

        void websocket_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            //MessageBox.Show(e.Exception.Message);
        }

        void websocket_Opened(object sender, EventArgs e)
        {
            //MessageBox.Show("Welcome, " + txtName.Text);
            ChatEntity model = new ChatEntity();
            model.UserName = txtName.Text;
            model.MessageType = MessageType.Login;

            websocket.Send(JsonConvert.SerializeObject(model));
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtName.Text))
            {
                MessageBox.Show("Please input name");
                return;
            }

            try
            {
                websocket.Open();
            }
            catch
            {
                return;
            }
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            try
            {
                ChatEntity model = new ChatEntity();
                model.UserName = txtName.Text;
                model.Token = token;
                model.MessageType = MessageType.Logout;

                websocket.Send(JsonConvert.SerializeObject(model));
            }
            catch
            {
                return;
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            ChatEntity model = new ChatEntity();
            model.UserName = txtName.Text;
            model.Token = token;
            model.MessageType = MessageType.Message;
            model.Message = txtSend.Text;

            websocket.Send(JsonConvert.SerializeObject(model));
            txtSend.Text = string.Empty;
        }

        private void txtName_TextChanged(object sender, EventArgs e)
        {
            btnConnect.Enabled = !string.IsNullOrEmpty(txtName.Text);
        }

        private void txtSend_TextChanged(object sender, EventArgs e)
        {
            btnSend.Enabled = !string.IsNullOrEmpty(txtSend.Text);

            if (!string.IsNullOrEmpty(txtSend.Text))
            {
                ChatEntity model = new ChatEntity();
                model.UserName = txtName.Text;
                model.Token = token;
                model.MessageType = MessageType.Typing;

                websocket.Send(JsonConvert.SerializeObject(model));
            }
        }

        delegate void AppendTextDelegate(TextBox txtCtrl, string text);

        private void AppendText(TextBox txtCtrl, string text)
        {
            if (txtCtrl.InvokeRequired)
            {
                txtCtrl.Invoke(new AppendTextDelegate(this.AppendText), new object[] { txtCtrl, text });
            }
            else
            {
                txtCtrl.Text = txtCtrl.Text += text;
            }
        }

        delegate void SetLabelTextDelegate(Label lblCtrl, string text);

        private void SetLabelText(Label lblCtrl, string text)
        {
            if (lblCtrl.InvokeRequired)
            {
                lblCtrl.Invoke(new SetLabelTextDelegate(this.SetLabelText), new object[] { lblCtrl, text });
            }
            else
            {
                lblCtrl.Text = text;
            }
        }

        delegate void SetControlEnabledDelegate(Control ctrl, bool enabled);

        private void SetControlEnabled(Control ctrl, bool enabled)
        {
            if (ctrl.InvokeRequired)
            {
                ctrl.Invoke(new SetControlEnabledDelegate(this.SetControlEnabled), new object[] { ctrl, enabled });
            }
            else
            {
                ctrl.Enabled = enabled;
            }
        }

        delegate void SetControlFocusDelegate(Control ctrl);

        private void SetControlFocus(Control ctrl)
        {
            if (ctrl.InvokeRequired)
            {
                ctrl.Invoke(new SetControlFocusDelegate(this.SetControlFocus), new object[] { ctrl });
            }
            else
            {
                ctrl.Focus();
            }
        }

        delegate void SetTextBoxTextDelegate(TextBox txtCtrl, string text);

        private void SetTextBoxText(TextBox txtCtrl, string text)
        {
            if (txtCtrl.InvokeRequired)
            {
                txtCtrl.Invoke(new SetTextBoxTextDelegate(this.SetTextBoxText), new object[] { txtCtrl, text });
            }
            else
            {
                txtCtrl.Text = text;
            }
        }

        private void tmrStatus_Tick(object sender, EventArgs e)
        {
            try
            {
                if (statuses.Count > 0)
                {
                    string status = string.Empty;
                    List<StatusEntity> newEntities = new List<StatusEntity>();
                    foreach (StatusEntity stat in statuses)
                    {
                        if (stat.Time - 100 != 0)
                        {
                            status += stat.UserName + ", ";
                            newEntities.Add(new StatusEntity() { UserName = stat.UserName, Time = stat.Time - 100 });
                        }
                    }

                    statuses = newEntities;

                    if (!string.IsNullOrEmpty(status))
                    {
                        status = status.Substring(0, status.Length - 2);
                        if (statuses.Count > 1)
                            status += " are typing...";
                        else
                            status += " is typing...";
                    }

                    SetLabelText(lblStatus, status);
                }
                else
                    SetLabelText(lblStatus, string.Empty);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
