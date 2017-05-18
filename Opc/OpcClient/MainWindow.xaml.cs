using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using OpcLib.Common;
using OpcClient.Implement;
using Newtonsoft.Json;
using OpcClient.Form;
using System.Threading.Tasks;

namespace OpcClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>   
    public partial class MainWindow : Window
    {
        private ClientWinsService _opcService;
        private bool _disConnect;
        private bool _serverStop;
        private bool _doStop;
        private BackgroundWorker _worker;
        private List<ListTagModel> _listTags;
        private bool _exit = true;

        delegate void ChangeEnableButton_Callback(string value);

        private enum NodeType
        { 
            Server=1,
            Group=2
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _disConnect = true;
            _serverStop = false;
            _doStop = false;

            _opcService = new ClientWinsService();
            _opcService.OnConnect += _opcService_OnConnect;

            btnConnect.IsEnabled = true;
            btnDisConnect.IsEnabled = false;

            _worker = new BackgroundWorker();
            _worker.DoWork += Woker_DoWork;
            _worker.RunWorkerCompleted += Woker_RunWorkerCompleted;
            _worker.ProgressChanged += Woker_ProgressChanged;
            _worker.WorkerSupportsCancellation = true;
            _worker.WorkerReportsProgress = true;

            btnConnect_Click(null, null);
            //this.WindowState= WindowState.Minimized;
        }

        private void _opcService_OnConnect(bool isConnected)
        {
            this.Cursor = null;
            _disConnect = !isConnected;
            //_serverStop = false;
            _doStop = false;
            btnConnect.IsEnabled = !isConnected;
            btnDisConnect.IsEnabled = isConnected;
            if(isConnected && !_worker.IsBusy)
                _worker.RunWorkerAsync();
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Cursor = Cursors.Wait;
                btnConnect.IsEnabled = false;
                btnDisConnect.IsEnabled = false;
                _opcService.Connect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Woker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                if (_disConnect) return;
                _opcService.GetListServer();
                var worker = sender as BackgroundWorker;
                while (!_disConnect)
                {
                    if (worker.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }
                    var st = _opcService.ReadValue();   //json string
                    if (st != null)
                    {
                        var txt = st.ToString();
                        if (txt=="") continue;
                        var objJson = JsonConvert.DeserializeObject(txt);
                        var requestType = int.Parse(((Newtonsoft.Json.Linq.JValue)(((Newtonsoft.Json.Linq.JProperty)((Newtonsoft.Json.Linq.JContainer)objJson).First).Value)).Value.ToString());
                        switch (requestType)
                        {
                            case (int)RequestType.ReadTag:
                                var tag = JsonConvert.DeserializeObject<GroupResponseModel>(txt);
                                worker.ReportProgress(0, tag);
                                break;
                            case (int)RequestType.GetListServer:
                                var retServers = JsonConvert.DeserializeObject<ServerListResponseModel>(txt);
                                worker.ReportProgress(0, retServers.Servers);
                                break;
                            case (int)RequestType.ReadGroupTags:
                                var tags = JsonConvert.DeserializeObject<GroupResponseModel>(txt);
                                worker.ReportProgress(0, tags);
                                break;
                            case (int)RequestType.RemoveTag:
                                var tagRemove = JsonConvert.DeserializeObject<TagRequestModel>(txt);
                                worker.ReportProgress(0, tagRemove);
                                break;
                            case (int)RequestType.AddTag:
                                var tagAdd = JsonConvert.DeserializeObject<TagRequestModel>(txt);
                                worker.ReportProgress(0, tagAdd);
                                break;
                            case (int)RequestType.AddGroup:
                                var groupAdd = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
                                worker.ReportProgress(0, groupAdd);
                                break;
                            case (int)RequestType.RemoveGroup:
                                var groupRemove = JsonConvert.DeserializeObject<GroupRequestModel>(txt);
                                worker.ReportProgress(0, groupRemove);
                                break;
                            case (int)RequestType.AddServer:
                                var serverAdd = JsonConvert.DeserializeObject<ServerRequestModel>(txt);
                                worker.ReportProgress(0, serverAdd);
                                break;
                            case (int)RequestType.RemoveServer:
                                var serverRemove = JsonConvert.DeserializeObject<ServerRequestModel>(txt);
                                worker.ReportProgress(0, serverRemove);
                                break;
                            case (int)RequestType.Error:
                                var error = JsonConvert.DeserializeObject<ErrorResponseModel>(txt);
                                worker.ReportProgress(0, error);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        _disConnect = _opcService.CloseConnection();
                        _serverStop = true;
                    }
                }
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                _serverStop = true;
            }
        }

        private void Woker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.UserState == null)
            {
                return;
            }
            else if (e.UserState.GetType() == typeof(TagRequestModel))
            {
                var tag = e.UserState as TagRequestModel;
                if (tag.RequestType == RequestType.RemoveTag)
                    DisplayRemoveTag(tag);
                else if (tag.RequestType == RequestType.AddTag)
                    DisplayAddTag(tag);
            }
            else if (e.UserState.GetType() == typeof(List<ServerInfoModel>))
            {
                DisplayListServer(e.UserState as List<ServerInfoModel>);
            }
            else if (e.UserState.GetType() == typeof(GroupResponseModel))
            {
                var info = e.UserState as GroupResponseModel;
                if (info.RequestType == RequestType.ReadTag)
                {
                    DisplayTags_AutoUpdate(info);      //DisplayText(tag);
                }
                else if (info.RequestType == RequestType.ReadGroupTags)
                {
                    var group = new GroupInfoModel() { Server = info.Server, Group = info.Group, Tags = info.Tags };
                    DisplayTags(group);
                }
            }
            else if (e.UserState.GetType() == typeof(ErrorResponseModel))
            {
                var info = e.UserState as ErrorResponseModel;
                MessageBox.Show(info.Message,"Server message", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else if (e.UserState.GetType() == typeof(GroupRequestModel))
            {
                var info = e.UserState as GroupRequestModel;
                if (info.RequestType == RequestType.AddGroup)
                    DisplayAddGroup(info);
                else if (info.RequestType == RequestType.RemoveGroup)
                    DisplayRemoveGroup(info);
            }
            else if (e.UserState.GetType() == typeof(ServerRequestModel))
            {
                var info = e.UserState as ServerRequestModel;
                if (info.RequestType == RequestType.AddServer)
                    DisplayAddServer(info);
                else if (info.RequestType == RequestType.RemoveServer)
                    DisplayRemoveServer(info);
            }
        }

        private void Woker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try 
            {
                if (e.Cancelled == true && _serverStop)
                    throw new Exception( "Server has stopped!");
                else if (e.Error != null)
                    throw new Exception(e.Error.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
             }
            _disConnect = _opcService.CloseConnection();
            btnConnect.IsEnabled = true;
            btnDisConnect.IsEnabled = false;
            if (_serverStop && !_doStop)
            {
                btnConnect_Click(null,null);
            }
        }

        private void btnDisConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_disConnect)
                {
                    _doStop = true;
                    _disConnect = _opcService.CloseConnection();
                    //_channel.ShutdownAsync().Wait();
                    btnConnect.IsEnabled = true;
                    btnDisConnect.IsEnabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_exit)
            {
                _disConnect = _opcService.CloseConnection();
            }
            else
            {
                e.Cancel = true;
                this.WindowState = WindowState.Minimized;
            }
        }

        private void mnuSetValue_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var info = lvTag.SelectedItem as ListTagModel;
                if (info == null)
                    throw new Exception("Please, choose an item");
                var inputDialog = new dlgInputDialog("Please, input new value:", "");
                if (inputDialog.ShowDialog() == true)
                {
                    var value = inputDialog.Answer;
                    int k;
                    if(!int.TryParse(value,out k))
                        throw  new Exception("Please, input a number");
                    var success = _opcService.WriteTag(info.Server.Name, info.Group.Name, info.Name, value);
                    if (!success)
                        MessageBox.Show("Can not write");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void mnuAddTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var node = treeGroup.SelectedItem as TreeViewItem;
                //node group
                if (node == null || (NodeType)node.Tag != NodeType.Group)
                    throw new Exception("Please, select a group");
                var dlg = new dlgInputDialog("Please input Tag name:", ""); 
                if (dlg.ShowDialog() == true)
                {
                    var tagName = dlg.Answer;
                    if (tagName == "")
                        throw new Exception("Please input Tag name");
                    var nParent = node.Parent as TreeViewItem;
                    var info = new TagInfoModel()
                    {
                        Server = new ServerModel() {Name = nParent.Header.ToString()},
                        Group = new GroupModel() {Name = node.Header.ToString()},
                        Tag = new TagModel() {Name = tagName}
                    };
                    var success = _opcService.AddTag(info);
                    if (!success)
                        MessageBox.Show("Can not write");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void mnuRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var info = lvTag.SelectedItem as ListTagModel;
                if (info == null)
                    throw new Exception("Please, choose an item");
                var success = _opcService.RemoveTag(info);
                if (!success)
                    MessageBox.Show("Can not send request");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #region my private func

        private void DisplayRemoveTag(TagRequestModel tag)
        {
            try
            {
                if (_listTags == null || _listTags.Count == 0) return;
                var groupd = _listTags[0].Group.Name;
                var server = _listTags[0].Server.Name;

                if (tag.Group.Name != groupd || tag.Server.Name != server) return;
                if (!_listTags.Exists(x => x.Name == tag.Tag.Name)) return;
                var item = _listTags.Single(x => x.Name == tag.Tag.Name);
                _listTags.Remove(item);
                lvTag.Items.Refresh();
            }
            catch
            {
            }
        }
        private void DisplayAddTag(TagRequestModel tag)
        {
            try
            {
                bool isNew = false;
                if (_listTags == null)
                {
                    _listTags = new List<ListTagModel>();
                    isNew = true;
                }
                _listTags.Add(new ListTagModel()
                {
                    Name = tag.Tag.Name,
                    Value = tag.Tag.Value,
                    Quality=tag.Tag.Quality,
                    Server = tag.Server,
                    Group = tag.Group
                });
                if(isNew)
                    lvTag.ItemsSource = _listTags;
                lvTag.Items.Refresh();
            }
            catch
            {
            }
        }
        private void DisplayAddGroup(GroupRequestModel model)
        {
            try
            {
                var newNode = new TreeViewItem();
                newNode.Header = model.GroupName;
                newNode.Tag = NodeType.Group;

                var node = treeGroup.SelectedItem as TreeViewItem;
                if ((NodeType)node.Tag == NodeType.Server)
                {
                    node.Items.Add(newNode);
                    node.IsExpanded = true;
                }
                else
                {
                    var parent = node.Parent as TreeViewItem;
                    parent.Items.Add(newNode);
                    parent.IsExpanded = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void DisplayRemoveGroup(GroupRequestModel model)
        {
            try
            {
                var node = treeGroup.SelectedItem as TreeViewItem;
                var parent = node.Parent as TreeViewItem;
                parent.Items.Remove(node);
                if (_listTags != null)
                {
                    _listTags.Clear();
                    lvTag.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void DisplayAddServer(ServerRequestModel model)
        {
            try
            {                
                var newNode = new TreeViewItem();
                newNode.Header = model.ServerName;
                newNode.Tag = NodeType.Server;
                treeGroup.Items.Add(newNode);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void DisplayRemoveServer(ServerRequestModel model)
        {
            try
            {
                var node = treeGroup.SelectedItem;
                treeGroup.Items.Remove(node);
                if (_listTags != null)
                {
                    _listTags.Clear();
                    lvTag.Items.Refresh();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void lvTag_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SetEnableMenu();
        }

        private void lvTag_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetEnableMenu();
        }
        
        private void DisplayTags(GroupInfoModel tags)
        {
            try
            {
                _listTags = new List<ListTagModel>();
                if (tags.Tags != null)
                {
                    foreach (var t in tags.Tags)
                    {
                        _listTags.Add(new ListTagModel()
                        {
                            Name = t.Name,
                            Value = t.Value,
                            Quality = t.Quality,
                            Server = new ServerModel() {Name = tags.Server.Name, IsConnected = tags.Server.IsConnected},
                            Group = new GroupModel() {Name = tags.Group.Name, Active = tags.Group.Active},
                            IconRow = "Image/dauthapdo.png"
                        });
                    }
                }
                var data = lvTag.ItemsSource;
                if (data != null) {
                    var temp = data as List<ListTagModel>;
                    temp.Clear();
                }
                lvTag.ItemsSource = _listTags;
                lvTag.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DisplayTags_AutoUpdate(GroupResponseModel info)
        {
            try
            {
                if (_listTags == null || _listTags.Count == 0) return;
                var group = _listTags[0].Group.Name;
                var server = _listTags[0].Server.Name;

                if (info.Group.Name != group || info.Server.Name != server) return;
                foreach (var tag in info.Tags)
                {
                    if (_listTags.Exists(x => x.Name == tag.Name))
                    {
                        var item=_listTags.Find(x => x.Name == tag.Name);
                        item.Value = tag.Value;
                        item.Quality = tag.Quality;
                    }
                }
                lvTag.Items.Refresh();                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DisplayListServer(List<ServerInfoModel> servers)
        {
            try
            {
                TreeViewItem firstNodeGroup=null;
                treeGroup.Items.Clear();
                foreach (var s in servers)
                {
                    TreeViewItem nServer = new TreeViewItem();
                    nServer.Tag = NodeType.Server;
                    nServer.Header = s.Server.Name;
                    foreach (var g in s.Groups)
                    {
                        TreeViewItem nGroup = new TreeViewItem() { Header = g.Name, Tag = NodeType.Group };
                        //nGroup.Selected += NGroup_Selected;
                        nServer.Items.Add(nGroup);
                        if (firstNodeGroup == null)
                            firstNodeGroup = nGroup;
                    }
                    nServer.IsExpanded = true;                    
                    treeGroup.Items.Add(nServer);
                }
                if (_serverStop)
                {
                    _serverStop = false;
                    if(firstNodeGroup!=null)
                        firstNodeGroup.IsSelected = true;
                }
            }
            catch (Exception ex)
            {
            }
        }        

        //private void NGroup_Selected(object sender, RoutedEventArgs e)
        //{
        //    var item = (TreeViewItem) sender;
        //    var group = new GroupTagsRequestModel()
        //    {
        //        RequestType = RequestType.ReadGroupTags,
        //        ServerId = int.Parse(((TreeViewItem) item.Parent).Tag.ToString()),
        //        GroupId = int.Parse(item.Tag.ToString())
        //    };
        //    _opcService.ReadGroupTags(group);
        //}

        //private void DisplayText(TagRequestModel tag)
        //{
        //    try
        //    {
        //        if (_listTags == null || _listTags.Count==0) return;
        //        var group = _listTags[0].Group.Name;
        //        var server = _listTags[0].Server.Name;

        //        if (tag.Group.Name != group || tag.Server.Name!=server) return;
        //        if (!_listTags.Exists(x => x.Name == tag.Tag.Name)) return;
        //        _listTags.Find(x => x.Name == tag.Tag.Name).Value = tag.Tag.Value;
        //        lvTag.Items.Refresh();
        //    }
        //    catch
        //    {
        //    }
        //}

        private void ChangeEnableButton(string value)
        {
            try
            {
                var btn = (Button)this.FindName(value);
                if (btn == null) return;
                if (!btn.Dispatcher.CheckAccess())
                {
                    var d = new ChangeEnableButton_Callback(ChangeEnableButton);
                    btn.Dispatcher.BeginInvoke(d, value);
                }
                else
                {
                    btn.IsEnabled = !btn.IsEnabled;
                }
            }
            catch(Exception ex)
            {
            }
        }

        private void treeGroup_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue == null) return;
            var node = e.NewValue as TreeViewItem;
            //node group
            if ((NodeType)node.Tag == NodeType.Group)
            {
                var nParent = node.Parent as TreeViewItem;
                var group = new GroupRequestModel()
                {
                    RequestType = RequestType.ReadGroupTags,
                    ServerName = nParent.Header.ToString(),
                    GroupName = ""+node.Header
                };
                _opcService.ReadGroupTags(group);
            }
            SetEnable_TreeMenu();
        }
        private void treeGroup_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            SetEnable_TreeMenu();
        }
        
        private void SetEnableMenu()
        {
            mnuAddTag.IsEnabled = false;
            mnuRemoveTag.IsEnabled = false;
            mnuSetValue.IsEnabled = false;
            if (_disConnect) return;
            var node = treeGroup.SelectedItem as TreeViewItem;
            if (node==null) 
                return;
            else if((NodeType)node.Tag == NodeType.Server)
            {
                if (node.Items.Count == 0) return;
            }
            mnuAddTag.IsEnabled = true;
            if (lvTag.SelectedIndex != -1)
            {
                mnuRemoveTag.IsEnabled = true;
                mnuSetValue.IsEnabled = true;
            }
        }
        private void SetEnable_TreeMenu()
        {
            mnuAddServer.IsEnabled = false;
            mnuRemoveServer.IsEnabled = false;
            mnuAddGroup.IsEnabled = false;
            mnuRemoveGroup.IsEnabled = false;
            if (_disConnect) return;
            if (treeGroup.SelectedItem == null)
            {
                mnuAddServer.IsEnabled = true;
            }
            else
            { 
                var node= treeGroup.SelectedItem as TreeViewItem;
                if ((NodeType)node.Tag == NodeType.Server)
                {
                    mnuAddServer.IsEnabled = true;
                    mnuRemoveServer.IsEnabled = true;
                    mnuAddGroup.IsEnabled = true;
                }
                else
                {
                    mnuAddGroup.IsEnabled = true;
                    mnuRemoveGroup.IsEnabled = true;                
                }
            }
        }
        #endregion

        private void mnuAddGroup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new dlgInputDialog("Please input Group name:", "");
                if (dlg.ShowDialog() == false) return;
                if (dlg.Answer == "")
                    throw new Exception("Please input Group name");
                var node = treeGroup.SelectedItem as TreeViewItem;
                var group = new GroupRequestModel();
                group.RequestType = RequestType.AddGroup;
                group.GroupName = dlg.Answer;
                if ((NodeType)node.Tag == NodeType.Server)
                {
                    group.ServerName = node.Header.ToString();
                }
                else
                {
                    var parent = node.Parent as TreeViewItem;
                    group.ServerName = parent.Header.ToString();
                }
                _opcService.AddGroup(group);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void mnuRemoveGroup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var node = treeGroup.SelectedItem as TreeViewItem;
                var group = new GroupRequestModel();
                group.RequestType = RequestType.RemoveGroup;
                group.GroupName = node.Header.ToString();
                
                var parent = node.Parent as TreeViewItem;
                group.ServerName = parent.Header.ToString();
                _opcService.RemoveGroup(group);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void mnuAddServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new dlgInputDialog("Please input Server name:", "");
                if (dlg.ShowDialog() == false) return;
                if (dlg.Answer == "")
                    throw new Exception("Please input Server name");
                var server = new ServerRequestModel() { RequestType = RequestType.AddServer, ServerName = dlg.Answer };
                _opcService.AddServer(server);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void mnuRemoveServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var node = treeGroup.SelectedItem as TreeViewItem;
                if(node==null) return;
                var server = new ServerRequestModel() { RequestType = RequestType.RemoveServer, ServerName = node.Header.ToString() };
                _opcService.RemoveServer(server);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Info", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
       
        private void MnuOpenApp_Click(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void MnuExitApp_Click(object sender, RoutedEventArgs e)
        {
            _exit = true;
            this.Close();
        }

        private void Notify1_TrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            //if (this.WindowState == WindowState.Minimized)
            //    this.Hide();
        }
    }
}
