using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using Archtoolkit.ATImport.Utils;
using AmbiensServer.LocalClient;
using AmbiensServer.Server;
using System.Linq;
using ambiens.avrs.view;
using ambiens.avrs.controller;
using ambiens.avrs.model;
using ambiens.utils.loader;
using ambiens.avrs.loader;
using ambiens.avrs;
using ambiens.utils.common;
using Newtonsoft.Json;
using UnityEditor.SceneManagement;
using System.Diagnostics;

namespace Archtoolkit.ATImport
{
    public class ProjectWindow : ATImportWindowBase
    {
        public static ProjectWindow projectWindow;
        public List<MATProject> avrsProjects = new List<MATProject>();
     
        public static string projectsPath
        {
            get
            {
                return Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                         "AmbiensPlugins"), "Projects");
            }
        }

        private string jsonPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                      "AmbiensPlugins") + "/user.dat";
            }
        }

        private Vector2 scrollviewProject = Vector2.zero;
        private bool userIsLogged = false;
        private bool rememberUser;
        private string nameText, passText = "";
        private static TempScene tempScene
        {
            set
            {
                _tempScene = value;
            }
        }
        private static TempScene _tempScene;
        private Texture2D syncTexture;
        private bool isCalculateNormalAssigned = false;

        public Avrs avrsChanged;
        private bool isAlreadyEnabled = false;
        private bool isCurrentlyImporting;
         

        private int downloadType = 2;
        private int pluginVersion = 11;


        [MenuItem("Tools/Ambiens/ArchToolkit/AT+Sync")]
        public static void Init()
        {
            var window = (ProjectWindow)EditorWindow.GetWindow(typeof(ProjectWindow), false, "AT+Sync");

            window.maxSize = new Vector2(600, 600);
            window.minSize = new Vector2(600, 600);

            projectWindow = window;

            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += this.CustomUpdate;

            if (isAlreadyEnabled)
                return;

            if (EditorApplication.isPlaying)
                return;

            if (EditorPrefs.HasKey(PrefsKeys.REMEMBER_USER_KEY))
                this.rememberUser = EditorPrefs.GetBool(PrefsKeys.REMEMBER_USER_KEY);

            if (string.IsNullOrEmpty(projectsPath))
                this.AssignProjects();


            if (EditorGUIUtility.isProSkin)
                this.syncTexture = Resources.Load<Texture2D>("Editor/UI/Sync");
            else
                this.syncTexture = Resources.Load<Texture2D>("Editor/UI/SyncBlack");

            this.SyncUser();

            isAlreadyEnabled = true;
#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged += this.AssignProjects;
#endif

        }

        public void AssignProjects()
        {
            if (avrsChanged != null)
                return;

            foreach (var item in this.avrsProjects)
            {
                if (item == null)
                    continue;

                if (item.views.Exists(a => a.isImporting))
                    return;
            }

            this.avrsProjects = this.GetProjects(projectsPath);
        }

        private void OnDestroy()
        {
            EditorApplication.update -= this.CustomUpdate;
#if UNITY_2018_1_OR_NEWER
            EditorApplication.projectChanged -= this.AssignProjects;
#endif
        }
        
        private void CustomUpdate()
        {
            if (RuntimeMeshLoader.IsInstanced())
            {
                RuntimeMeshLoader.Instance.saveMesh = ATSettings.saveMesh;

                RuntimeMeshLoader.Instance.Update();

                this.isCalculateNormalAssigned = false;
            }
            else
            {
                this.isCurrentlyImporting = false;

                if (!this.isCalculateNormalAssigned)
                {
                    foreach (var item in GameObject.FindObjectsOfType<VMesh>())
                    {
                        if (item != null)
                        {
                            if (item.GetComponent<RecalculateNormalsComponent>() == null)
                            {
                                var normalCompoenent = item.gameObject.AddComponent<RecalculateNormalsComponent>();

                                if (ATSettings.setNormalToZero)
                                    normalCompoenent.RecalculateNormals(0);
                            }
                        }
                    }
                    this.isCalculateNormalAssigned = true;
                }
            }
            if (RuntimeMeshDataOrderedLoader.IsInstanced())
            {
                RuntimeMeshDataOrderedLoader.Instance.Update();
            }
            if (RuntimeTextureLoader.IsInstanced())
            {
                RuntimeTextureLoader.Instance.Update();
            }
            if (RuntimeJSonLoader.IsInstanced())
            {
                RuntimeJSonLoader.Instance.Update();
            }

            if (this.avrsChanged != null)
            {
                if (this.avrsChanged.isOpened)
                {
                    this.avrsChanged.isImporting = true;
                    Import(this.avrsChanged);
                }
                this.avrsChanged = null;
            }
        }

        private void ProjectChanged(Avrs avrsChanged)
        {
            this.avrsChanged = avrsChanged;
        }

        private bool CheckInstantiationPercentage()
        {
            float tPerc = 0;
            float mPerc = 0;
            if (RuntimeMeshLoader.IsInstanced())
            {
                mPerc = (float)RuntimeMeshLoader.Instance.requested / (float)RuntimeMeshLoader.Instance.toRequest;
            }
            if (RuntimeTextureLoader.IsInstanced())
            {
                tPerc = 1;
                if (RuntimeTextureLoader.Instance.toRequest != 0)
                    tPerc = (float)RuntimeTextureLoader.Instance.requested / (float)RuntimeTextureLoader.Instance.toRequest;
            }
            if (tPerc == 1 && mPerc == 1)
            {
                return true;
            }
            else return false;
        }

        private void SyncUser()
        {
            if (!Directory.Exists(projectsPath))
                Directory.CreateDirectory(projectsPath);

            userIsLogged = false;

         

            string str = JsonConvert.SerializeObject(new Dictionary<string, string> { { "type", "" + downloadType } });
#if false
            bool isPluginUpdated = true;

            Server.POST<MResources>(Server.GETLASTVERSIONBYTYPE, str,
                                       (MResources res) =>
                                       {
                                           if (res == null)
                                               isPluginUpdated = false;
                                           else if (res.versionID > pluginVersion)
                                           {
                                               string content = "A new update is available, please download the latest version";
                                               if (res.Mandatory)
                                               {
                                                   content = "a new mandatory update is available, please download the latest version in order to use AT+Sync";
                                           }
                                           EditorUtility.DisplayDialog("New Update Avaiable!", content, "Ok");
                                           Help.BrowseURL("https://www.archtoolkit.com/dashboard");
                                           isPluginUpdated = false;
                                           this.Close();
                                       }
                                       else
                                           isPluginUpdated = true;

                                       
                                   },
                                   (string error) =>
                                   {
                                       EditorUtility.DisplayDialog("Error", error, "I understand");
                                       isPluginUpdated = false;
                                   }, true);    

            if (!isPluginUpdated)
            {
                this.Close();
                return;
            }
#endif
            if (File.Exists(jsonPath))
            {
                var decripted = AmbiensServer.Utils.StringUtils.Decrypt(File.ReadAllText(jsonPath));
                var appUser = Newtonsoft.Json.JsonConvert.DeserializeObject<ApplicationUser>(decripted);
                new Client(appUser); // Crea client sharato con la dll
                this.Repaint();
                if (appUser != null)
                {
                    HttpReadyRequest.SyncUser((ApplicationUser user) =>
                    {
                        new Client(appUser); // Crea client sharato con la dll
                        this.Repaint();
                        if (user.PasswordHash == appUser.PasswordHash && user.UserName == appUser.UserName)
                        {
                            CheckLicense((bool licenseValid) =>
                            {
                                if (licenseValid)
                                {
                                    this.AssignProjects();
                                    userIsLogged = true;
                                    this.Repaint();
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Error", "License expired!", "Ok");
                                    Help.BrowseURL("https://www.archtoolkit.com/dashboard/Payments");
                                    this.Close();
                                }
                            });
                        }
                    },
                    (string error) =>
                    {
                        EditorUtility.DisplayDialog("Error", error, "Ok");
                    }, true);
                }
            }

        }

        private void ShowProject()
        {
            this.ApplyLogo();

            // Create the main area
            GUILayout.BeginArea(new Rect(0, 150, this.position.width, this.position.height - 2), this.backgroundStyle);

            GUILayout.Space(20);

            GUILayout.BeginHorizontal();

            var lStyle = new GUIStyle(GUI.skin.label);

            lStyle.fontSize = 16;
            lStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("click on the project you need to import into the scene", lStyle);

            if (GUILayout.Button(this.syncTexture, GUILayout.Width(32), GUILayout.Height(32)))
                this.AssignProjects();

            GUILayout.EndHorizontal();


            GUILayout.BeginVertical();

            GUILayout.Space(20);

            this.scrollviewProject = GUILayout.BeginScrollView(this.scrollviewProject, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUILayout.Width(this.position.width), GUILayout.Height(this.position.height - 150));

            this.DrawProjectField();

            GUILayout.EndScrollView();

            GUILayout.EndVertical();

            GUILayout.EndArea();
        }

        private void DrawProjectField()
        {
            var labelSkin = new GUIStyle(GUI.skin.label);

            labelSkin.alignment = TextAnchor.UpperLeft;
            labelSkin.fontStyle = FontStyle.Normal;
            labelSkin.clipping = TextClipping.Overflow;
            labelSkin.fontSize = 11;

            var toggleSkin = new GUIStyle(GUI.skin.toggle);

            toggleSkin.alignment = TextAnchor.UpperLeft;

            foreach (var p in this.avrsProjects)
            {
                if (p == null)
                    continue;

                GUILayout.BeginVertical(GUI.skin.box);

                p.isOpenedInUI = EditorGUILayout.Foldout(p.isOpenedInUI, p.projectName, true);
                
                if (p.isOpenedInUI)
                {
                    if (p.views != null)
                    {
                        foreach (var avrs in p.views)
                        {
                            if (avrs == null)
                                continue;

                            var horizontal = EditorGUILayout.BeginHorizontal(GUI.skin.box);

                            GUILayout.Label(avrs.thumbnail, GUILayout.Width(32), GUILayout.Height(32));

                            var rectBar = EditorGUILayout.BeginVertical();

                            if (!Client.Instance.isSubscriptionFree)
                            {
                                var syncChanged = avrs.autoSync;

                                avrs.autoSync = GUILayout.Toggle(avrs.autoSync, "Auto sync");

                                if (syncChanged != avrs.autoSync)
                                {
                                    avrs.SetSyncTo(avrs.autoSync);
                                }
                            }
                            else
                                avrs.autoSync = false;

                            GUILayout.Label(avrs.name, labelSkin);

                            string projectPathToSee = avrs.projectPath;

                            if (avrs.projectPath.Length > 100) // in this way we avoid to have a label too long
                                projectPathToSee = "..." + avrs.projectPath.Substring(100);
                            else if (avrs.projectPath.Length > 50)
                                projectPathToSee = "..." + avrs.projectPath.Substring(50);

                            GUILayout.Label(projectPathToSee, labelSkin);

                            GUILayout.Space(20);

                            if (avrs.isImporting)
                            {
                                if (!RuntimeMeshLoader.IsInstanced())
                                {
                                    avrs.isImporting = false;
                                }

                                if (RuntimeMeshLoader.IsInstanced())
                                {

                                    float perc = (float)RuntimeMeshLoader.Instance.requested / (float)RuntimeMeshLoader.Instance.toRequest;

                                    var progressField = new Rect(12, rectBar.yMax - 15, rectBar.width + 36, 15);

                                    EditorGUI.ProgressBar(progressField, perc, "Importing... " + RuntimeMeshLoader.Instance.requested + "/" + RuntimeMeshLoader.Instance.toRequest);

                                    this.Repaint();
                                }
                            }

                            EditorGUILayout.EndVertical();

                            if (!avrs.isImporting && !this.isCurrentlyImporting)
                            {
                                var import = GUILayout.Button(this.syncTexture, GUILayout.Height(32), GUILayout.Width(32));

                                if (import)
                                {
                                    avrs.isImporting = true;
                                    Import(avrs);
                                }
                            }

                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                GUILayout.EndVertical();
            }
        }

        private void ShowLogin()
        {
            var loginBackgroundTwoStyle = new GUIStyle(GUI.skin.box);
            loginBackgroundTwoStyle.alignment = TextAnchor.MiddleCenter;
            loginBackgroundTwoStyle.normal.background = TextureUtils.MakeTex(1, 1, Color.gray);

            var loginBackgroundImage = new Texture2D(1, 1);
            loginBackgroundImage.SetPixel(0, 0, new Color(125, 124, 32, 1));
            loginBackgroundImage.Apply();
            loginBackgroundImage = Resources.Load<Texture2D>(ATImportDataPath.WINDOW_LOGO_LOGIN_PATH);

            var loginBackgroundStyle = new GUIStyle(GUI.skin.box);
            loginBackgroundStyle.alignment = TextAnchor.MiddleCenter;
            loginBackgroundStyle.normal.background = loginBackgroundImage;

            // Create the left area
            GUILayout.BeginArea(new Rect(10, 10, 270, 580), loginBackgroundStyle);

            GUILayout.EndArea();

            // Create the right area
            GUILayout.BeginArea(new Rect(290, 10, 300, 580));

            // GUILayout.BeginVertical();

            GUILayout.Space(50);

            var loginLogoImage = new Texture2D(1, 1);
            loginLogoImage.SetPixel(0, 0, new Color(125, 124, 32, 1));
            loginLogoImage.Apply();
            loginLogoImage = Resources.Load<Texture2D>(ATImportDataPath.WINDOW_LOGO_PATH);

            var loginLogoStyle = new GUIStyle(GUI.skin.box);
            loginLogoStyle.clipping = TextClipping.Clip;
            loginLogoStyle.imagePosition = ImagePosition.ImageAbove;
            loginLogoStyle.margin = new RectOffset(50, 50, 1, 1);
            loginLogoStyle.alignment = TextAnchor.MiddleCenter;
            loginLogoStyle.normal.background = loginLogoImage;

            GUILayout.Box(string.Empty, loginLogoStyle, GUILayout.Width(200), GUILayout.Height(100));

            // ACCOUNT LOGIN SECTION
            GUILayout.Space(50);

            var accountLoginLabelStyle = new GUIStyle(GUI.skin.label);
            accountLoginLabelStyle.alignment = TextAnchor.MiddleCenter;
            accountLoginLabelStyle.fontSize = 23;
            accountLoginLabelStyle.margin = new RectOffset(0, 0, 0, 0);
            GUILayout.Label("ACCOUNT LOGIN", accountLoginLabelStyle);

            GUILayout.Space(15);

            var textfieldStyle = new GUIStyle(GUI.skin.textField);

            GUILayout.BeginHorizontal();

            GUILayout.Space(50);

            this.nameText = GUILayout.TextField(this.nameText, textfieldStyle, GUILayout.Width(200), GUILayout.Height(20));

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            GUILayout.Space(50);

            this.passText = GUILayout.PasswordField(this.passText, "*"[0], textfieldStyle, GUILayout.Width(200), GUILayout.Height(20));

            GUILayout.EndHorizontal();

            GUILayout.Space(50);

            GUILayout.BeginHorizontal();

            GUILayout.Space(70);

            this.rememberUser = GUILayout.Toggle(this.rememberUser, "Rememeber me");

            EditorPrefs.SetBool(PrefsKeys.REMEMBER_USER_KEY, this.rememberUser);

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            GUILayout.BeginHorizontal();

            GUILayout.Space(70);


            var defaultColor = GUI.backgroundColor;
            GUI.backgroundColor = Color.cyan;

            if (GUILayout.Button("Login", GUILayout.Width(150), GUILayout.Height(30)))
                this.LoginUser(this.nameText, this.passText);

            GUI.backgroundColor = defaultColor;

            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();

            GUILayout.Space(10);

            GUIStyle signupStyle = new GUIStyle(GUI.skin.label);
            GUIContent txt = new GUIContent("Sign up");
            var b = signupStyle.border;

            b.left = 0;
            b.top = 0;
            b.right = 0;
            b.bottom = 0;
            signupStyle.alignment = TextAnchor.MiddleCenter;
            signupStyle.fontStyle = FontStyle.Italic;
            signupStyle.contentOffset = new Vector2(signupStyle.contentOffset.x - 5, signupStyle.contentOffset.y);

            if (GUILayout.Button(txt, signupStyle))
                Application.OpenURL("https://www.archtoolkit.com/dashboard");

            GUILayout.EndVertical();

            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(390, 565, 100, 50));

            GUILayout.BeginHorizontal();

            var socialButtonsStyle = new GUIStyle(GUI.skin.button);
            //socialButtonsStyle.border = new RectOffset(0,0,0,0);
            socialButtonsStyle.imagePosition = ImagePosition.ImageOnly;
            socialButtonsStyle.alignment = TextAnchor.MiddleCenter;
            socialButtonsStyle.normal.background = null;

            if (GUILayout.Button(Resources.Load<Texture2D>(ATImportDataPath.FACEBOOK_LOGO_PATH), socialButtonsStyle, GUILayout.Width(30), GUILayout.Height(30)))
                Help.BrowseURL("https://www.facebook.com/groups/152547958672484/");
            if (GUILayout.Button(Resources.Load<Texture2D>(ATImportDataPath.INSTAGRAM_LOGO_PATH), socialButtonsStyle, GUILayout.Width(30), GUILayout.Height(30)))
                Help.BrowseURL("https://www.instagram.com/ambiensvr/");
            if (GUILayout.Button(Resources.Load<Texture2D>(ATImportDataPath.YOUTUBE_LOGO_PATH), socialButtonsStyle, GUILayout.Width(30), GUILayout.Height(30)))
                Help.BrowseURL("https://www.youtube.com/channel/UCkqMEDTMuARl75aCK5T5ryA");

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void OnGUI()
        {
            if (EditorApplication.isPlaying)
                return;

            if (this.userIsLogged)
                this.ShowProject();
            else
                this.ShowLogin();
        }

        private void LoginUser(string name, string pass)
        {
            var appUser = new ApplicationUser();
            appUser.UserName = name;
            appUser.PasswordHash = pass;

            new Client(appUser);

            HttpReadyRequest.LoginUser(appUser, (ApplicationUser user) =>
            {
                new Client(user); // Crea client sharato con la dll
                CheckLicense((bool licenseValid) =>
                {
                    if (licenseValid)
                    {
                        if (this.rememberUser)
                        {
                            File.WriteAllText(jsonPath, AmbiensServer.Utils.StringUtils.Encrypt(Newtonsoft.Json.JsonConvert.SerializeObject(user)));
                        }

                        userIsLogged = true;
                        this.Repaint();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "License expired!", "Ok");
                        Help.BrowseURL("https://www.archtoolkit.com/dashboard/Payments");
                    }
                });

                this.Repaint();
            },
            (string error) =>
            {
                EditorUtility.DisplayDialog("Error", error, "Ok");
                this.Repaint();
            }, true);
        }

        private void CheckLicense(Action<bool> OnCheckComplete)
        {
            HttpReadyRequest.GetCustomer((List<MCustomer> customers) =>
            {
                if (customers != null && customers.Count > 0)
                {
                    if (customers[0].subscriptions != null && customers[0].subscriptions.Count > 0)
                    {
                        if (!customers[0].subscriptions[0].IsActive())
                            OnCheckComplete(false);
                        else
                        {
                            Client.Instance.isSubscriptionFree = customers[0].subscriptions[0].isFreeVersion;

                            OnCheckComplete(true);
                        }
                    }
                    else
                    {
                        Client.Instance.isSubscriptionFree = customers[0].subscriptions[0].isFreeVersion;

                        OnCheckComplete(false);
                    }
                }

            }, (string error) =>
            {
                UnityEngine.Debug.Log(error);
            }, true);
        }

        public List<MATProject> GetProjects(String path, bool orderByRecent = false)
        {
            List<MATProject> projects = new List<MATProject>();

            foreach (var dir in Directory.GetDirectories(path))
            {
                var directoryInfo = new DirectoryInfo(dir);

                var project = new MATProject(Path.GetFileName(dir), new FileInfo(dir));

                projects.Add(project);

                var files = directoryInfo.GetFiles("*.avrs", SearchOption.AllDirectories).OrderBy(f => f.CreationTime);

                foreach (var file in files)
                {
                    if (file == null)
                        continue;

                    var thumbs = Directory.GetFiles(file.DirectoryName, "*.jpg", SearchOption.TopDirectoryOnly);

                    Texture2D thumb = new Texture2D(1, 1);

                    if (thumbs.Length > 0)
                    {
                        if (File.Exists(thumbs[0]))
                        {
                            var bytes = File.ReadAllBytes(thumbs[0]);

                            thumb.LoadImage(bytes);

                            thumb.Apply();
                        }
                        else
                            UnityEngine.Debug.LogWarning("Project is without thumbnail");

                    }

                    var view = new Avrs(file.FullName, thumb);

                    view.OnProjectChanged = this.ProjectChanged;

                    var tScene = TempScene.Get(file.FullName);

                    project.views.Add(view);
                }
            }

            projects = projects.OrderByDescending(f => f.fileInfo.LastWriteTime).ToList();

            projects[0].isOpenedInUI = true; // First is always open

            return projects;
        }

        public void Import(Avrs project)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            if (!string.IsNullOrEmpty(project.projectPath))
            {
                this.isCurrentlyImporting = true;
                _tempScene = TempScene.Get(project.projectPath); // Check if scene already exist

                var deserializer = new AvrsDeserializer();
                deserializer.DeserializeFile(project.projectPath,
                    (MScene scene) =>
                    {
                        EditingInstanceController.Instance.AddSceneController(deserializer.sceneController);

                        new BimDataComponent(deserializer.sceneController);


                        if (project.isOpened)
                            project.root = GameObject.Find(project.name + "_Root");

                        if (project.root == null)
                        {
                            project.root = new GameObject(project.name + "_Root");
                        }

                        if (_tempScene == null)
                        {
                            PrepareToSync(project, scene, null, deserializer);

                            _tempScene = TempScene.Create(project.projectPath);

                            _tempScene.Initialize(scene, project.projectPath, deserializer.sceneController);
                        }
                        else
                        {
                            PrepareToSync(project, scene, _tempScene.mScene, deserializer);

                        }

                    },
                    (float p) => { },
                    (string error) => { });
            }
        }

        private void PrepareToSync(Avrs project, MScene newScene, MScene oldScene, AvrsDeserializer deserializer)
        {
            this.Repaint();

            if (oldScene == null || project.root.transform.childCount == 0)
            {
                // Instanzia scena
                deserializer.InstantiateScene(project.root.transform,
                (GameObject go) =>
                {

                },
                (float p) =>
                {

                },
                (string e) =>
                {
                    UnityEngine.Debug.Log("error " + e);
                }
                );

            }
            else
            {
                // Sync

                //s.mScene.assets.materialData.Clear();
                _tempScene.mScene.assets.textureData.Clear();
                _tempScene.mScene.assets.meshData.Clear();

                SyncScene(project.root, ref newScene, ref deserializer);

                /*foreach (var vScene in viewsInScene)
                {
                    if (vScene == null)
                        continue;

                    var model = deserializer.sceneController.scene.mObjectWrapper.Find(m => m.id == vScene.model.id);

                    if (model != null)
                    {
                        model = vScene.model;
                    }
                }

                _tempScene.mScene = deserializer.sceneController.scene;
                _tempScene.currentSceneController = deserializer.sceneController;*/
            }
        }

        private void SyncScene(GameObject root, ref MScene newSceneModel, ref AvrsDeserializer deserializer)
        {
            Sync(root, deserializer, newSceneModel.mObjectWrapper);
        }

        private List<T> GetChildrenComponents<T>(List<Transform> childs) where T : Component
        {
            List<T> components = new List<T>();

            foreach (Transform child in childs)
            {
                if (child.gameObject == null)
                    continue;

                var component = child.gameObject.GetComponent<T>();

                if (component != null)
                    components.Add(component);
            }

            return components;
        }

        private void Sync(GameObject root, AvrsDeserializer deserializer, List<MAmbiensObject> models)
        {
            GameObject gOParent = root;

            if (root == null)
                return;

            //Prendo qui la lista di view
            var currentViewList = this.GetChildrenComponents<VAmbiensObject>(root.transform.GetChildrenUtils().ToList());

            //Per ogni modello che viene da revit...
            for (int i = 0; i < models.Count; i++)
            {
                var newModel = models[i];

                GameObject go = null;
                VAmbiensObject currView = null;

                //...trovo la view corrispondente già instanziata
                foreach (var instantiatedView in currentViewList)
                {
                    if (instantiatedView.model.id == newModel.id)
                    {
                        currView = instantiatedView;
                        go = currView.gameObject;
                    }

                }
                //Se NON ho trovato currView e quindi non ho trovato la view corrispondente..
                if (currView == null)
                {
                    //... istanzio il gameobject giusto
                    deserializer.sceneController.objectsController.InstantiateViewFromModel(ref newModel, root.transform,
                        (GameObject gO) =>
                        {

                        }, (float p) => { }, (string error) => { });
                    continue;
                }
                //Se invece ho trovato il matching tra model da revit e view corrispondente
                else
                {
                    //questo non l'ho capito...

                    //currView.model = deserializer.sceneController.objectsController.AddFromExisting(go);

                    // Aggiorno il model della view con quello nuovo
                    //currView.model = newModel;

                    currentViewList.Remove(currView);

                    go.transform.position = newModel.p.V3FromFloatArray();
                    go.transform.rotation = Quaternion.Euler(newModel.r.V3FromFloatArray());
                    go.transform.localScale = newModel.s.V3FromFloatArray();

                    // Refresh Components
                    var bimDatas = go.GetComponents<BimData>();

                    foreach (var bim in bimDatas)
                    {
                        if (bim == null)
                            continue;

                        GameObject.DestroyImmediate(bim);
                    }

                    for (int componentCount = 0; componentCount < newModel.c.Count; componentCount++)
                    {
                        var comp = newModel.c[componentCount];

                        deserializer.sceneController.componentsController.InstantiateViewFromModel(ref comp, go, null, null, null);
                    }

                    if (currView.model.meshID != -1)
                    {
                        var vMesh = go.GetComponent<VMesh>();

                        if (vMesh != null && vMesh.model != null)
                        {
                            var newMesh = deserializer.sceneController.scene.assets.meshData.Find(m => m.uniqueMeshID == vMesh.model.uniqueMeshID);

                            if (newMesh != null && !string.IsNullOrEmpty(newMesh.url))
                            {
                                vMesh.model.url = newMesh.url;

                                if (vMesh.model.url.StartsWith(CScene.tilde))
                                {
                                    vMesh.model.localUrl = vMesh.model.url.Replace(CScene.tilde, deserializer.sceneController.ProjectPath);
                                }

                                if (newMesh.hashChecker != vMesh.model.hashChecker)
                                {

                                    //Debug.Log("updating mesh " + newModel.name + "  " + vMesh.model.hashChecker + "  " + newMesh.hashChecker);
                                    vMesh.GetGraphics((MeshFilter f) =>
                                    {
                                        if (f != null)
                                        {

                                            var meshCollider = vMesh.GetComponent<MeshCollider>();
                                            if (meshCollider != null)
                                            {
                                                meshCollider.sharedMesh = f.sharedMesh;
                                            }
                                        }
                                    }, (float p) => { }, (string error) => { });
                                }

                                // Aggiorno il model di vmesh con il nuovo model meshdata
                                vMesh.model = newMesh;
                            }
                        }
                    }
                }

                if (newModel.childs.Count > 0)
                    this.Sync(go, deserializer, newModel.childs);
            }

            //DOPO IL CICLO, se restano dei GO nella lista, vanno cancellati
            while (currentViewList.Count > 0)
            {
                var item = currentViewList[0];

                currentViewList.Remove(item);

                GameObject.DestroyImmediate(item.gameObject);

            }
        }
    }

}
