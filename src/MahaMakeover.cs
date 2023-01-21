using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using SideLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OutwardModTemplate
{
    [BepInPlugin(GUID, NAME, VERSION)]
    public class MyMod : BaseUnityPlugin
    {
        // Choose a GUID for your project. Change "myname" and "mymod".
        public const string GUID = "maha.mahasmakeover";
        // Choose a NAME for your project, generally the same as your Assembly Name.
        public const string NAME = "Mahasmakeover";
        // Increment the VERSION when you release a new version of your mod.
        public const string VERSION = "1.0.0";
        public const string SLPackID = "mahasmakeover";

        // For accessing your BepInEx Logger from outside of this class (MyMod.Log)
        internal static ManualLogSource Log;

        public static ConfigEntry<bool> EnableGlowingHair;
        public static ConfigEntry<float> HairIntensity;
        public static ConfigEntry<Color> CustomHairColor;
        public static ConfigEntry<bool> CustomHairGlows;
        private SLPack SLPack;

        // Awake is called when your plugin is created. Use this to set up your mod.
        internal void Awake()
        {
            Log = this.Logger;

            EnableGlowingHair = Config.Bind(NAME, "EnableGlowingHair", true, "Enable Glowing Hair options? Requires restart");
            HairIntensity = Config.Bind(NAME, "HairIntensity", 7f, "How bright the glowing hair is, warning values over 10 might melt your eyes. Requires restart");
            CustomHairColor = Config.Bind(NAME, "CustomHairColor",  Color.yellow, "You can define a custom hair color here in hexadecimal color format. Requires restart");
            CustomHairGlows = Config.Bind(NAME, "CustomHairGlows", true, "Should CustomHairColor use HairIntensity value? (Should it glow or not) Requires restart");
            SL.OnPacksLoaded += SL_OnPacksLoaded;
        }

        private void SL_OnPacksLoaded()
        {
           // Log.LogMessage("MaleHeadsWhite");

            #region Male Heads
            UpdateMaterials(CharacterManager.CharacterVisualsPresets.MHeadsWhite);
            //Log.LogMessage("MaleHeadsAsian");
            UpdateMaterials(CharacterManager.CharacterVisualsPresets.MHeadsAsian);
            //Log.LogMessage("MaleHeadsBlack");
            UpdateMaterials(CharacterManager.CharacterVisualsPresets.MHeadsBlack);
            #endregion

            //Log.LogMessage("FemaleHeadsWhite");
            #region Female Heads      
            UpdateMaterials(CharacterManager.CharacterVisualsPresets.FHeadsWhite);
            //Log.LogMessage("FemaleHeadsAsian");
            UpdateMaterials(CharacterManager.CharacterVisualsPresets.FHeadsAsian);
            //Log.LogMessage("FemaleHeadsBlack");
            UpdateMaterials(CharacterManager.CharacterVisualsPresets.FHeadsBlack);
            #endregion

            //Log.LogMessage("Bodies");
            UpdateMaterials(CharacterManager.CharacterVisualsPresets.MSkins);
            UpdateMaterials(CharacterManager.CharacterVisualsPresets.FSkins);


            //Log.LogMessage("Hairs");
            UpdateMaterials(CharacterManager.CharacterVisualsPresets.Hairs);

            AddNewHairMaterialToPresets("Custom Color", CustomHairGlows.Value ? GetHDRColor(CustomHairColor.Value, HairIntensity.Value) : CustomHairColor.Value);

            if (EnableGlowingHair.Value)
            {
                AddNewHairMaterialToPresets("Shiny Yellow", GetHDRColor(Color.yellow, HairIntensity.Value));
                AddNewHairMaterialToPresets("Shiny Red", GetHDRColor(Color.red, HairIntensity.Value));
                AddNewHairMaterialToPresets("Shiny Blue", GetHDRColor(Color.blue, HairIntensity.Value));
                AddNewHairMaterialToPresets("Shiny Cyan", GetHDRColor(Color.cyan, HairIntensity.Value));
                AddNewHairMaterialToPresets("Shiny Purple", GetHDRColor(Color.magenta, HairIntensity.Value));
                AddNewHairMaterialToPresets("Shiny White", GetHDRColor(Color.white, HairIntensity.Value));
                AddNewHairMaterialToPresets("Shiny Green", GetHDRColor(Color.green, HairIntensity.Value));
            }
        }

        private void UpdateMaterials(Material[] Materials)
        {
            foreach (var item in Materials)
            {
                Texture2D texture2D = GetTexture(item.name);
                Texture2D NormalMap = GetTextureNormal(item.name);

                if (texture2D != null)
                {
                    UpdateTexture(item, texture2D, NormalMap);
                }
            }
        }

        private void UpdateMaterials(GameObject[] GameObjects)
        {
            foreach (var item in GameObjects)
            {
                SkinnedMeshRenderer meshRenderer = item.gameObject.GetComponent<SkinnedMeshRenderer>();

                if (meshRenderer)
                {
                    Texture2D texture2D = GetTexture(item.name);
                    Texture2D NormalMap = GetTextureNormal(item.name);

                    if (texture2D != null)
                    {
                        UpdateTexture(meshRenderer.material, texture2D, NormalMap);
                    }
                }
            }
        }

        /// a HDR Color in Unity is a RGBA 0-1 Color multiplied by an intensity value
        /// <summary>
        /// Mutliplies Color by Intensity clamping alpha between 0-1
        /// </summary>
        /// <param name="Color"></param>
        /// <param name="intensity"></param>
        /// <returns></returns>
        private Color GetHDRColor(Color Color, float intensity)
        {
            Color *= intensity;

            Color.a = Mathf.Clamp01(Color.a);
            return Color;
        }
        /// <summary>
        /// Creates a new material and appends it to the CharacterVisualsPresets.HairMaterials, optionally enables emission.
        /// </summary>
        /// <param name="Material"></param>
        /// <param name="MaterialName"></param>
        /// <param name="NewColor"></param>
        /// <param name="AddEmissionKeyWord"></param>
        private void AddNewHairMaterialToPresets(string MaterialName, Color NewColor, bool AddEmissionKeyWord = true)
        {
            //uses the first hair material as the base, they are all from the same texture atlas regardless.
            if (CharacterManager.CharacterVisualsPresets.HairMaterials[0] == null)
            {
                return;
            }

            Material NewMaterial = new Material(CharacterManager.CharacterVisualsPresets.HairMaterials[0]);
            NewMaterial.name = MaterialName;
            NewMaterial.color = NewColor;

            if (AddEmissionKeyWord)
            {
                NewMaterial.SetShaderKeywords(new string[] {
                    "_EMISSION"
                });
            }

            CharacterManager.CharacterVisualsPresets.HairMaterials = CharacterManager.CharacterVisualsPresets.HairMaterials.AddToArray(NewMaterial);
        }
        /// <summary>
        /// Attempts to retrieve a texture loaded by SL from the SideLoader/Texture2D folder.
        /// </summary>
        /// <param name="modelName"></param>
        /// <returns></returns>
        private Texture2D GetTexture(string modelName)
        {
            SLPack = SL.GetSLPack(SLPackID);
            if (SLPack == null)
            {
                Log.LogMessage($"Could not find SLPack");
                return null;
            }


            if (SLPack.Texture2D.ContainsKey(modelName))
            {
                return SLPack.Texture2D[modelName];
            }


            //Log.LogMessage($"SLPack does not contain a key for {modelName}");
            return null;
        }
        private Texture2D GetTextureNormal(string modelName)
        {
            SLPack = SL.GetSLPack(SLPackID);
            if (SLPack == null)
            {
                Log.LogMessage($"Could not find SLPack");
                return null;
            }


            if (SLPack.Texture2D.ContainsKey(modelName + "_N"))
            {
                return SLPack.Texture2D[modelName + "_N"];
            }

            return null;
        }
        private void UpdateTexture(Material Material, Texture2D NewMainTexture, Texture2D NormalMap = null)
        {
            if (NewMainTexture == null)
            {
                return;
            }

            Material.mainTexture = NewMainTexture;

            if (NormalMap != null)
            {
                Material.SetTexture("_NormTex", NormalMap);
            }

        }
    }
}
