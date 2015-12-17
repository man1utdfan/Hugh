﻿using HughLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web.Http;

namespace Hugh.Services
{
    class HueLightService
    {
        public static async Task<List<Light>> RetrieveLights()
        {
            var cts = new CancellationTokenSource();
            List<Light> retVal = new List<Light>();
            cts.CancelAfter(5000);

            try
            {
                HttpClient client = new HttpClient();
                string ip, username;
                int port;
                SettingsService.RetrieveSettings(out ip, out port, out username);
                var response = await client.GetAsync(new Uri(string.Format("http://{0}:{1}/api/{2}/lights/", ip, port, username))).AsTask(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    cts.Cancel();    
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                //System.Diagnostics.Debug.WriteLine(jsonResponse);

                retVal = ParseJson(jsonResponse);
            }
            catch (Exception)
            {
                cts.Cancel();
            }
            return retVal;
        }

        public static async Task<string> RetrieveUsername()
        {
            Boolean hasGotUsername = false;
            string jsonResponse = "";
            string usernameRetrieved = "";
            while (!hasGotUsername)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    HttpStringContent content = new HttpStringContent("{\"devicetype\":\"HueApp#ComfyCrew\"}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                    string ip, username;
                    int port;
                    SettingsService.RetrieveSettings(out ip, out port, out username);
                    var response = await client.PostAsync(new Uri(string.Format("http://{0}:{1}/api/", ip, port)), content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return string.Empty;
                    }

                    jsonResponse = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(jsonResponse);

                    JsonArray jsonArray = JsonArray.Parse(jsonResponse);
                    ICollection<string> keys = jsonArray.First().GetObject().Keys;
                    if (keys.Contains("error"))
                    {
                        Hugh.Views_Viewmodels.MainPage.ShowErrorDialogue();
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        hasGotUsername = true;
                        JsonObject succesObject = jsonArray.First().GetObject();

                        System.Diagnostics.Debug.WriteLine(succesObject.Values.First().GetObject().Values.First().GetString());
                        usernameRetrieved = succesObject.Values.First().GetObject().Values.First().GetString();
                    }

                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
            return usernameRetrieved;
        }

        public static List<Light> ParseJson(string json)
        {
            List<Light> lights = new List<Light>();

            JsonObject jsonObject = JsonObject.Parse(json);
            foreach (string key in jsonObject.Keys)
            {
                string lightId = key;

                if (lightId.Equals("error"))
                {
                    System.Diagnostics.Debug.WriteLine(jsonObject[key]);
                    continue;
                }

                JsonObject lightToAdd;
                Light l = null;

                try
                {
                    lightToAdd = jsonObject.GetNamedObject(key, null);

                    JsonObject lightState = lightToAdd.GetNamedObject("state", null);
                    if (lightState != null)
                    {
                        Light.Effect effect = Light.Effect.EFFECT_NONE;
                        if (lightState.GetNamedString("effect", string.Empty).Equals("colorloop"))
                            effect = Light.Effect.EFFECT_COLORLOOP;

                        l = new Light(Convert.ToInt32(lightId), lightToAdd.GetNamedString("name", string.Empty), lightState.GetNamedBoolean("on", false),
                                        Convert.ToInt32(lightState.GetNamedNumber("hue", 0)), Convert.ToInt32(lightState.GetNamedNumber("sat", 255)),
                                        Convert.ToInt32(lightState.GetNamedNumber("bri", 255)), effect, lightState.GetNamedBoolean("reachable", false));
                    }
                    else
                        l = new Light(Convert.ToInt32(lightId), string.Format("Hue lamp {0}", lightId), true, 20000, 255, 255, Light.Effect.EFFECT_NONE, true);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0} - {1}", e.Message, e.StackTrace));
                    continue;
                }

                if (l != null)
                    lights.Add(l);
            }

            return lights;
        }
    }
}
