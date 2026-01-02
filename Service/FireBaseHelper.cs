using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Gms.Extensions;
using Android.Gms.Tasks;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Big17DataFirebase2.Model;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Java.Lang;
using Java.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Big17DataFirebase2.Service
{
	public class FireBaseHelper
	{
		protected static FireBaseHelper me;
		private FirebaseApp app;

		readonly static string TAG = "KOSTYAPP";
		public static TaskCompletionListeners taskCompletionListeners;
		
		static FireBaseHelper() { me = new FireBaseHelper(); }

		protected FireBaseHelper() { InitializeFirebase(); }

		//Initialize Firebase app
		private void InitializeFirebase()
		{
			try
			{
				//1.
				//Parse Firebase json file:
				//Install Newtonsoft.Json NuGet latest version
				//Rename json file google-services.json to googleservices.json 
				//Place json file google-services.json into Root/Assets
				//Set its Build Action in Property to "AndroidAsset"	

				string json;
				string projectId = "";
				string apiKey = "";
				string storageBucket = "";
				AssetManager assets = Application.Context.Assets;
				using (Stream stream = assets.Open("googleservices.json")) //Correct way to access raw resource
				{
					// Reading from app data directory
					using (StreamReader r = new StreamReader(stream))
					{
						json = r.ReadToEnd();

						//using Newtonsoft.Json.Linq;
						//JObject.Parse(json) parses the JSON string into a JObject, making it easy to navigate the JSON structure.
						//JToken is used to access the individual elements within the JSON.
						JObject jsonObj = JObject.Parse(json);
						JToken projectInfo = jsonObj["project_info"];

						if (projectInfo != null)
						{
							projectId = (string)projectInfo["project_id"];
							storageBucket = (string)projectInfo["storage_bucket"];
						}
						else
						{
							Log.Error(TAG, "project_info is null");
							return; //Exit, as we cannot continue without project_info
						}

						JToken client = jsonObj["client"][0]; // Access the client array
						apiKey = (string)client["api_key"][0]["current_key"];
					}
				}

				//2. Initilize Firebase App
				app = FirebaseApp.InitializeApp(Application.Context); //using Firebase;
				if (app == null)
				{
					var options = new FirebaseOptions.Builder()
					.SetProjectId(projectId)
					.SetApplicationId(projectId)
					.SetApiKey(apiKey)
					.SetDatabaseUrl(projectId + ".firebaseapp.com")
					.SetStorageBucket(storageBucket)
					.Build();

					app = FirebaseApp.InitializeApp(Application.Context, options);
				}
			}
			catch (FileNotFoundException ex)
			{
				Android.Util.Log.Error(TAG, $"File not found: {ex.Message}");
			}
			catch (System.Exception ex)
			{
				Android.Util.Log.Error(TAG, $"Error parsing JSON: {ex.Message}");
			}
		}

		#region Users
		public static async Task<string> SignInUserAsync(string uemail, string upass)
		{
			try
			{
				FirebaseAuth mAuth = FirebaseAuth.Instance;
				//using Android.Gms.Extensions;
				await mAuth.SignInWithEmailAndPassword(uemail, upass);
				Log.Debug(TAG, $"MyApp: User Auth {uemail} SignIn success");
				return mAuth.CurrentUser.Uid; // Indicate success
			}
			catch (FirebaseAuthException ex)
			{
				Log.Error(TAG, $"SignInUserAsync: User Auth SignIn failed: {ex.Message}");
				return null; // Indicate failure
			}
			catch (System.Exception ex)
			{
				Log.Error(TAG, $"SignInUserAsync: User Auth SignIn failed, general error: {ex.Message}");
				return null; // Indicate failure
			}
		}
		public static async Task<string> RegisterUserForAuth(User user)
		{
            try
            {
                FirebaseAuth mAuth = FirebaseAuth.Instance;
				//using Android.Gms.Extensions;
				await mAuth.CreateUserWithEmailAndPassword(user.UserEmail, user.UserPass);
                Log.Debug(TAG, $"RegisterUserForAuth: User Auth {user.UserEmail} SignIn success");
                return mAuth.CurrentUser.Uid; // Indicate success
            }
            catch (FirebaseAuthException ex)
            {
                Log.Error(TAG, $"SignInUserAsync: User Auth SignIn failed: {ex.Message}");
                return null; // Indicate failure
            }
            catch (System.Exception ex)
            {
                Log.Error(TAG, $"SignInUserAsync: User Auth SignIn failed, general error: {ex.Message}");
                return null; // Indicate failure
            }           
        }
        public static async Task<bool> InsertAsync(User user)
        {
            try
            {
                //Insert user to FireStore database
                HashMap userMap = new HashMap(); //using Java.Util;
                userMap.Put("FirstName", user.FirstName);
                //userMap.Put("IsAdmin", user.IsAdmin);
                userMap.Put("LastName", user.LastName);
                userMap.Put("UserEmail", user.UserEmail);
                userMap.Put("UserMobile", user.UserMobile);
                userMap.Put("UserPassword", user.UserPass);


                DocumentReference userReference = FirebaseFirestore.Instance
                                                                        .Collection("users")
                                                                        .Document(user.Id);
                await userReference.Set(userMap);
                Log.Debug(TAG, $"InsertAsync: Insert User to Firestore complited");
                return true; // Indicate success
            }
            catch (FirebaseFirestoreException ex)
            {
                Log.Error(TAG, $"InsertAsync: Insert User to Firestore failed: {ex.Message}");
                return false; // Indicate failure
            }
            catch (System.Exception ex)
            {
                Log.Error(TAG, $"MyApp: Insert User to Firestore failed: {ex.Message}");
                return false; // Indicate failure
            }
        }
        #endregion

        public class TaskCompletionListeners : Java.Lang.Object, IOnSuccessListener, IOnFailureListener //using Android.Gms.Tasks;
		{
			public event EventHandler<TaskSuccessEventArgs> Success;
			public event EventHandler<TaskCompletionFailureEventArgs> Failure;

			//Success
			public class TaskCompletionFailureEventArgs : EventArgs
			{
				public string Cause { get; set; }
			}
			//Failure
			public class TaskSuccessEventArgs : EventArgs
			{
				public Java.Lang.Object Result { get; set; }
			}
			public void OnFailure(Java.Lang.Exception e)
			{
				Failure?.Invoke(this, new TaskCompletionFailureEventArgs { Cause = e.Message });
			}

			public void OnSuccess(Java.Lang.Object result)
			{
				Success?.Invoke(this, new TaskSuccessEventArgs { Result = result });
			}
		}
	}
}