using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using System.Threading;


namespace MeanwhileInLosSantos
{
    //SOUND EFFECT////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class SoundEffect
    {
        string _soundFile;
        Thread _soundThread;
        bool _isStopped = true;
        public bool IsFinished { get { return _isStopped; } }

        public SoundEffect(string soundFile)
        {
            _soundFile = soundFile;
        }

        public void Play()
        {
            if (!_isStopped)
                return;

            _soundThread = new Thread(PlayThread);
            _soundThread.Start();
        }

        private void PlayThread()
        {
            _isStopped = false;
            System.Media.SoundPlayer player = new System.Media.SoundPlayer(_soundFile);
            player.PlaySync();
            _isStopped = true;
        }
    }
    //SCENE///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class Scene
    {
        public Vector3 SwitchLocation { get; set; }
        public String ModelID { get; set; }
        public String Wave { get; set; }
        public Action SceneTask { get; set; }
        public bool FixedCam { get; set; }
        public Vector3 FixedCamLocation { get; set; }
        public Vector3 FixedCamRotation { get; set; }

        public Scene(Vector3 switchLocation, String modelID, String wave, Action sceneTask, bool fixedCam, Vector3 fixedCamLocation, Vector3 fixedCamRotation)
        {
            SwitchLocation = switchLocation;
            ModelID = modelID;
            Wave = wave;
            SceneTask = sceneTask;
            FixedCam = fixedCam;
            FixedCamLocation = fixedCamLocation;
            FixedCamRotation = fixedCamRotation;
        }
    }

    public class MeanwhileInLosSantos : Script ///////////////////////////////////////////////////////////////////////////////////////////////////
    {
        bool modON = false; //a boolean to avoid the mod immediately starting at game load
        Ped newPed = null; //a ped to temporary transfer the player to another entity
        //variables to save info from the original ped before every switch
        Vector3 originalLocation; //a variable to store the original position of the player to teleport back to when we end the mod
        float originalHeading; //a variable to store the original heading of the player
        Model originalModel; //a variable to store the original model of the player character
        int totComponent = 13; //the total number of components: 0: Face 1: Mask 2: Hair 3: Torso 4: Leg 5: Parachute / bag 6: Shoes 7: Accessory 8: Undershirt 9: Kevlar 10: Badge 11: Torso 2
        List<int> drawableID = new List<int>(); //a list to save the IDs of drawables for each component variation of the current Ped model
        List<int> texturesID = new List<int>(); //a list to save the IDs of textures for each drawable of the component variation of the current Ped model
        List<int> paletteID = new List<int>(); //a list to save the IDs of palettes that we have no ideas what they are ;)
        int totProps = 5; //the toal number of props: 0: HATS, 1: GLASSES, 2: EARS, 6: WATCHES, 7: BRACELETS
        List<int> propNumbers = new List<int> { 0, 1, 2, 6, 7 };
        List<int> drawableIDprops = new List<int>();
        List<int> texturesIDprops = new List<int>();
        //a list to store all the possible weapons
        List<UInt32> Weapons = new List<UInt32> { 0x99B507EA, 0x678B81B1, 0x4E875F73, 0x958A4A8F, 0x440E4788, 0x84BD7BFD, 0x1B06D571, 0x5EF9FEC4, 0x22D8FE39, 0x99AEEB3B, 0x13532244, 0x2BE6766B, 0xEFE7E2DF, 0x0A3D4D34, 0xBFEFFF6D, 0x83BF0278, 0xAF113F99, 0x9D07F764, 0x7FD62962, 0x1D073A89, 0x7846A318, 0xE284C527, 0x9D61E50F, 0x3656C8C1, 0x5FC3C11, 0xC472FE2, 0xA284510B, 0x4DD2DC56, 0xB1CA77B1, 0x42BF8A85, 0x93E220BD, 0x2C3731D9, 0xFDBC8A50, 0xA0973D5E, 0x24B17070, 0x60EC506, 0x34A67B97, 0xBFD21232, 0xC0A3098D, 0xD205520E, 0x7F229F94, 0x63AB0442, 0xAB564B93, 0x787F0BB, 0x83839C4, 0x92A27487, 0x7F7497E5, 0xA89CB99E, 0xC734385A, 0x3AABBBAA, 0x61012683, 0xF9DCBF2D, 0x6D544C99, 0xA2719263 };
        List<UInt32> CarriedWeapons = new List<UInt32>(); //a list to store the weapons carried by the player
        List<int> CarriedAmmo = new List<int>(); //a list to store the ammo for each weapon carried by the player
        UInt32 CurrentWeapon; //the current weapon selected that gets stored before every switch is initiated
        bool CurrentWeaponEquipped; //a bool to store if the weapon selected is equipped
        int wantedLevel; //an int to store the number of stars/wanted level before every switch is initiated
        int poshHash;
        Model poshModel;
        
        //scenes
        bool backToOriginal = true; //a boolean to check if we are switching to the original scene or to the next NPC
        List<Scene> myScenes = new List<Scene>(); //a list to store all the scenes for each switch
        SoundEffect dialogue = new SoundEffect("./scripts/monologues/NPCsevolve.wav"); //a soundplayer to play each of the monologues
        bool isDialoguePlaying = false; //check if the sound file is playing
        bool isScenePlayed = false; //check if the scene is finished
        int index; //an int to go through the different scenes
        Camera myCam = null; //a custom camera positioned at different locations for every switch
        Vector3 myCamPos; //a 3D point to store the position of the camera in each scene
        
        //timer
        bool FuncInit = false; //bool to start the timer
        int TimerCountdown; //timer to trigger the switch

        public MeanwhileInLosSantos() ///////////////////////////////////////////////////////////////////////////////////////////////////
        {
            this.Tick += onTick;

            //each scene needs these parameters: switchLocation, character modelID, the audio wav filename, the task/action to be performed, a boolean to set if the camera is fixed on a custom location/rotation, the location of the camera if fixed, the rotation of the camera if fixed
            Scene DejaVu = new Scene(new Vector3(3432.546f, 5171.821f, 34.8f), "a_m_o_soucent_01", "r_04", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_DRINKING", Game.Player.Character.Position, 170f), true, new Vector3(3427.708f, 5167.628f, 36.8f), new Vector3(0.4f, 0f, -63f));//Willie's supermarket
            myScenes.Add(DejaVu); //OK

            Scene BitFrosty = new Scene(new Vector3(-722.7375f, -918.392f, 18f), "a_f_y_tourist_02", "h_03", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_STAND_IMPATIENT", Game.Player.Character.Position, 94.39f), true, new Vector3(-731.5458f, -919.2435f, 18.7f), new Vector3(0f, 0f, -87f)); //next to gasoline station
            myScenes.Add(BitFrosty); //OK

            Scene IcecreamSoGood = new Scene(new Vector3(549.6545f, 3362.689f, 98.9f), "a_m_y_business_01", "u_06", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_AA_SMOKE", Game.Player.Character.Position, 90f), true, new Vector3(542.8257f, 3361.611f, 99.7f), new Vector3(-3f, 0f, -84f));//top of hill
            myScenes.Add(IcecreamSoGood); //OK

            Scene IllusoryFreeWill = new Scene(new Vector3(2684.4f, 3274f, 54.25f), "u_f_m_corpse_01", "h_01", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_WINDOW_SHOP_BROWSE", Game.Player.Character.Position, 176f), true, new Vector3(2684f, 3280f, 55.5f), new Vector3(0f, 0f, 165f)); //gasoline station
            myScenes.Add(IllusoryFreeWill);  //OK perfect

            Scene InnerMonologue = new Scene(new Vector3(-815.6f, 5196.8f, 103.5f), "a_f_y_hippie_01", "j_06", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_DRUG_DEALER", Game.Player.Character.Position, 112.4f), true, new Vector3(-821.1f, 5195.1f, 104f), new Vector3(0f, 0f, -76.5f));//Under the bridge
            myScenes.Add(InnerMonologue); //OK

            Scene MainCharacter = new Scene(new Vector3(-975f, 216f, 65.5f), "a_m_m_tennis_01", "u_01", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_TENNIS_PLAYER", Game.Player.Character.Position, 90f), true, new Vector3(-976.2764f, 220.1424f, 66.5f), new Vector3(0f, 0f, -168f));//Balcony Industrial Rent Banner
            myScenes.Add(MainCharacter); //OK

            Scene QuantumSimulation = new Scene(new Vector3(1098f, -2605f, 13.3f), "a_m_y_motox_02", "u_04", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_AA_SMOKE", Game.Player.Character.Position, 213f), true, new Vector3(1109f, -2598f, 17f), new Vector3(0f, 0f, 127f)); //beach
            myScenes.Add(QuantumSimulation); //OK

            Scene RealLifeNPC = new Scene(new Vector3(-1300f, -1592f, 3.3f), "a_m_y_gencaspat_01", "b_02", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_POWER_WALKER", Game.Player.Character.Position, 29.5f), true, new Vector3(-1304.572f, -1586.261f, 5f), new Vector3(-8f, 0f, -143f));//top of hill
            myScenes.Add(RealLifeNPC); //OK

            Scene CantRememberFaces = new Scene(new Vector3(-1726.811f, 119.6574f, 63.35f), "a_m_m_hasjew_01", "u_07", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_PICNIC", Game.Player.Character.Position, 294.4621f), true, new Vector3(-1720.477f, 121.5735f, 64f), new Vector3(0f, 0f, 108f));//Track Course
            myScenes.Add(CantRememberFaces); //OK

            Scene SameCar = new Scene(new Vector3(-1120f, -1399f, 4.15f), "a_f_m_eastsa_02", "j_01", () => Game.Player.Character.Task.LookAt(myCamPos), true, new Vector3(-1116.282f, -1403.191f, 6.2f), new Vector3(0f, 0f, 25.2f)); //crossroad
            myScenes.Add(SameCar); //OKish

            Scene MainCharacter2 = new Scene(new Vector3(-975f, 216f, 65.5f), "a_m_m_tennis_01", "u_02", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_TENNIS_PLAYER", Game.Player.Character.Position, 90f), true, new Vector3(-976.2764f, 220.1424f, 66.5f), new Vector3(0f, 0f, -168f));//Balcony Industrial Rent Banner
            myScenes.Add(MainCharacter2); //OK

            Scene DungeonMaster = new Scene(new Vector3(-379.1991f, 386.6f, 107.5f), "s_m_y_construct_02", "u_05", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_CONST_DRILL", Game.Player.Character.Position, 109f), true, new Vector3(-376.4149f, 379.6793f, 108.5f), new Vector3(0f, 0f, 17f));//Construction Site
            myScenes.Add(DungeonMaster); //OK

            Scene MainCharacterOfYourOwnLife = new Scene(new Vector3(-1174.144f, -502.7513f, 39.3f), "a_f_y_vinewood_01", "j_02", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_STAND_MOBILE", Game.Player.Character.Position, 96f), true, new Vector3(-1180.132f, -508.9387f, 40f), new Vector3(0.4f, 0f, -40f));//Willie's supermarket
            myScenes.Add(MainCharacterOfYourOwnLife); //OK

            Scene SentientNPCs = new Scene(new Vector3(332.1471f, -207.0676f, 53.08632f), "a_f_y_gencaspat_01", "h_04", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_HANG_OUT_STREET", Game.Player.Character.Position, 67f), true, new Vector3(327.4f, -205.2f, 54.6f), new Vector3(0f, 0f, -111f));//Under the bridge
            myScenes.Add(SentientNPCs); //OK

            Scene Groceries = new Scene(new Vector3(-1547.7f, -441.5f, 34.9f), "a_m_y_soucent_04", "b_03", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_WINDOW_SHOP_BROWSE", Game.Player.Character.Position, 118f), true, new Vector3(-1549.585f, -435.4f, 37.67f), new Vector3(-11f, 0f, -153.7f));//top of hill
            myScenes.Add(Groceries); //OK

            Scene RecursiveSimulationGenerator = new Scene(new Vector3(603.8924f, -950.1597f, 9f), "a_f_y_soucent_01", "j_04", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_MUSCLE_FLEX", Game.Player.Character.Position, 90f), true, new Vector3(608.3513f, -949.6037f, 10.736f), new Vector3(7f, 0f, 92f));//Under the bridge
            myScenes.Add(RecursiveSimulationGenerator); //OK

            Scene Switzerland = new Scene(new Vector3(-1308.9f, -1377.4f, 3.5f), "a_f_o_soucent_02", "j_05", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_GUARD_STAND", Game.Player.Character.Position, 108.5f), true, new Vector3(-1318.27f, -1382.867f, 6.5f), new Vector3(0f, 0f, -70f));//Under the bridge
            myScenes.Add(Switzerland); //OK

            Scene FlatEarth = new Scene(new Vector3(-165.9269f, 3613.606f, 51f), "a_m_y_methhead_01", "b_01", () => Game.Player.Character.Task.StartScenario("CODE_HUMAN_MEDIC_KNEEL", Game.Player.Character.Position, 274f), true, new Vector3(-153.9f, 3612.8f, 53.1f), new Vector3(0f, 0f, 74f));//top of hill
            myScenes.Add(FlatEarth); //OK

            Scene Self = new Scene(new Vector3(1469.7f, 6349.9f, 22.8f), "a_f_y_eastsa_03", "j_03", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_COP_IDLES", Game.Player.Character.Position, 51.2f), true, new Vector3(1462.5f, 6353.5f, 25.8f), new Vector3(-10f, 0f, -127f));//Under the bridge
            myScenes.Add(Self);//OK

            Scene Multitasking = new Scene(new Vector3(-47.79f, 6534.815f, 30.5f), "a_m_m_hillbilly_01", "r_01", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_WINDOW_SHOP_BROWSE", Game.Player.Character.Position, 300f), true, new Vector3(-41f, 6538f, 32f), new Vector3(0.4f, 0f, 134f));//Willie's supermarket
            myScenes.Add(Multitasking); //OK

            Scene Multiplayer = new Scene(new Vector3(3620.384f, 5020.06f, 10.3f), "a_m_m_eastsa_01", "r_02", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_LEANING", Game.Player.Character.Position, 200f), true, new Vector3(3616.74f, 5013.311f, 12f), new Vector3(0.4f, 0f, -34f));//Willie's supermarket
            myScenes.Add(Multiplayer); //OK

            Scene PublicSolitude = new Scene(new Vector3(-1560.252f, -897.2117f, 9.2f), "a_m_o_genstreet_01", "r_03", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_WINDOW_SHOP_BROWSE", Game.Player.Character.Position, 51.2f), true, new Vector3(-1567.505f, -890.7684f, 12f), new Vector3(0f, 0f, -130f));//Willie's supermarket
            myScenes.Add(PublicSolitude); //OK

            Scene MainCharacter3 = new Scene(new Vector3(-975f, 216f, 65.5f), "a_m_m_tennis_01", "u_03", () => Game.Player.Character.Task.StartScenario("WORLD_HUMAN_TENNIS_PLAYER", Game.Player.Character.Position, 90f), true, new Vector3(-976.2764f, 220.1424f, 66.5f), new Vector3(0f, 0f, -168f));//Balcony Industrial Rent Banner
            myScenes.Add(MainCharacter3); //OK

            Scene FreeWill = new Scene(new Vector3(-15f, 321f, 111.8f), "a_f_y_business_01", "h_02", () => Game.Player.Character.Task.WanderAround(), false, new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 0f));//Swimming Pool
            myScenes.Add(FreeWill); //OK

            index = -1;

            //create Cam 
            myCam = World.CreateCamera(new Vector3(0f, 0f, 0f), Game.Player.Character.Rotation, 50f);
        }

        private void onTick(object sender, EventArgs e) //////////////////////////////////////////////////////////////////////////////////
        {
            //TIMER////////////////////////////////////////////////////////////////////////////////
            int elapsedTime = (int)(Game.LastFrameTime * 1000);
            if (index < myScenes.Count-1)
            {
                if (!FuncInit)
                {
                    Random rando = new Random();
                    int rndInterval = rando.Next(120000, 180000);//random timer between 2 and 3 minutes
                    TimerCountdown = rndInterval;
                    FuncInit = true;
                }
                else
                {
                    TimerCountdown -= elapsedTime;
                    if (TimerCountdown < 0)
                    {
                        //inititate switch here
                        initSwitch();
                        FuncInit = false;
                    }
                }
            }
            
            /////////////////////////////////////////////////////////////////////////////////////////
            if (!backToOriginal)
            {
                //hide HUD and RADAR
                Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
            }

            if (isDialoguePlaying && dialogue.IsFinished == true)
            {
                isDialoguePlaying = false;
                //we are switching back to the original game character
                backToOriginal = true;
                isScenePlayed = false;
                //switch to original location
                switchToOriginalSetup();
            }
            //If the character switch is in progress
            if (Function.Call<bool>(Hash.IS_PLAYER_SWITCH_IN_PROGRESS))
            {
                //hide HUD and RADAR
                Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);

                //If Switch State is 8 – that's the point when it starts dropping to the player 
                if (modON && Function.Call<int>(Hash.GET_PLAYER_SWITCH_STATE) == 8)
                {
                    if (backToOriginal)//if we are switching back to the original character setup
                    {
                        //Set the player to the switch location
                        Game.Player.Character.Position = originalLocation;
                        Game.Player.Character.Heading = originalHeading;
                        //Generate the hash for the original model
                        poshHash = originalModel;
                        //Create the model
                        poshModel = new Model(poshHash);
                    }
                    else //if we are switching to the next NPC
                    {
                        //Set the player to the switch location
                        Game.Player.Character.Position = myScenes[index].SwitchLocation;
                        //Generate the hash for the chosen model
                        poshHash = Game.GenerateHash(myScenes[index].ModelID);
                        //Create the model
                        poshModel = new Model(poshHash);
                    }

                    //Check if it is valid
                    if (poshModel.IsValid)
                    {
                        //Wait for it to load, should be okay because it was used to create the target ped
                        while (!poshModel.IsLoaded) { Wait(100); }
                        //Change the player model to the target ped model
                        Function.Call(Hash.SET_PLAYER_MODEL, Game.Player, poshHash);
                        //Let the game clean up the created Model
                        poshModel.MarkAsNoLongerNeeded();
                    }
                    else
                    {
                        //Falls to here if the model valid check fails
                        Function.Call(Hash.SET_PLAYER_MODEL, Game.Player, (int)PedHash.Tourist01AFY);
                    }
                    //Delete the target ped as it's no longer needed
                    newPed.Delete();

                    //update fixed cam info if we are using it
                    if (myScenes[index].FixedCam == true)
                    {
                        myCam.Position = myScenes[index].FixedCamLocation;
                        myCam.Rotation = myScenes[index].FixedCamRotation;
                    }

                    //Set the switch outro based on the gameplay camera position 
                    //Function.Call((Hash)0xC208B673CE446B61, camPos.X, camPos.Y, camPos.Z, camRot.X, camRot.Y, camRot.Z, camFOV, camFarClip, p8);
                    Function.Call(Hash.SET_PLAYER_SWITCH_OUTRO, GameplayCamera.Position.X, GameplayCamera.Position.Y, GameplayCamera.Position.Z, GameplayCamera.Rotation.X, GameplayCamera.Rotation.Y, GameplayCamera.Rotation.Z, GameplayCamera.FieldOfView, 500, 2);

                    //Call this unknown native that seems to finish things off
                    Function.Call(Hash.ALLOW_PLAYER_SWITCH_OUTRO);//Function.Call(Hash._0x74DE2E8739086740);

                    //if we are switching to the next NPC play the dialogue sound and scene animation
                    if (!backToOriginal)
                    {
                        //play the task
                        myScenes[index].SceneTask();////WHY DOES THIS NEED TO BE HERE AND CALLED MULTIPLE TIMES????

                        //play the dialogue (THIS GETS EXCUTED ONLY ONCE)
                        if (!isScenePlayed)
                        {
                            if (myScenes[index].FixedCam == true) World.RenderingCamera = myCam;
                            else World.RenderingCamera = null;

                            isScenePlayed = true;
                            //Load and play the dialogue of the character
                            dialogue = new SoundEffect("./scripts/monologues/" + myScenes[index].Wave + ".wav");//load a new dialogue
                            dialogue.Play();
                            isDialoguePlaying = true;
                        }
                    }
                    else //if we go back to the original Player Ped
                    {
                        //set wanted level
                        Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, wantedLevel, 0);
                        Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, 0);
                        //set the saved original variations
                        for (int i = 0; i < totComponent; i++)
                        {
                            Function.Call(Hash.SET_PED_COMPONENT_VARIATION, Game.Player.Character, i, drawableID[i], texturesID[i], paletteID[i], 2);
                        }
                        //set the saved original props
                        for (int i = 0; i < totProps; i++)
                        {
                            Function.Call(Hash.SET_PED_PROP_INDEX, Game.Player.Character, propNumbers[i], drawableIDprops[i], texturesIDprops[i], 1);
                        }
                        //set the saved originally carried weapons
                        for (int i = 0; i < CarriedWeapons.Count; i++)
                        {
                            Function.Call(Hash.GIVE_WEAPON_TO_PED, Game.Player.Character, CarriedWeapons[i], CarriedAmmo[i], true, false);
                        }
                        //equip current weapon
                        Function.Call(Hash.SET_CURRENT_PED_WEAPON, Game.Player.Character, CurrentWeapon, CurrentWeaponEquipped);
                        //clear all tasks and scenarios
                        Game.Player.Character.Task.ClearAll();
                    }
                }
            }
        }

        //inititate switch
        private void initSwitch()
        {
            if (index < myScenes.Count-1)
            {
                //if we are not in a mission, not in a car, and furhter than 75 m from the next NPC location
                if ((Function.Call<bool>(Hash.GET_MISSION_FLAG) == false) && (Function.Call<bool>(Hash.IS_PED_IN_ANY_VEHICLE, Game.Player.Character, true) == false) && (Game.Player.Character.Position.DistanceTo2D(myScenes[index + 1].SwitchLocation)) > 75)
                {
                    //get wanted level
                    wantedLevel = Function.Call<int>(Hash.GET_PLAYER_WANTED_LEVEL, Game.Player);
                    //set the wanted level to 0
                    Function.Call(Hash.SET_PLAYER_WANTED_LEVEL, Game.Player, 0, false);

                    //if we are switching from the game character setup store all the original info
                    if (backToOriginal)
                    {
                        //the first time we switch to a new NPC scene we store the player's position
                        Function.Call(Hash.SET_ENTITY_VISIBLE, Game.Player.Character, true, 0);

                        originalLocation = Game.Player.Character.Position;      //get position
                        originalHeading = Game.Player.Character.Heading;        //get heading
                        originalModel = Function.Call<int>(Hash.GET_ENTITY_MODEL, Game.Player.Character);//get model
                        //getting all original variations and props
                        texturesID.Clear();
                        drawableID.Clear();
                        paletteID.Clear();
                        texturesIDprops.Clear();
                        drawableIDprops.Clear();
                        for (int i = 0; i < totComponent; i++)
                        {
                            //get the drawableID
                            texturesID.Add(Function.Call<int>(Hash.GET_PED_TEXTURE_VARIATION, Game.Player.Character, i));
                            //get the textureID
                            drawableID.Add(Function.Call<int>(Hash.GET_PED_DRAWABLE_VARIATION, Game.Player.Character, i));
                            //get the paletteID
                            paletteID.Add(Function.Call<int>(Hash.GET_PED_PALETTE_VARIATION, Game.Player.Character, i));
                        }
                        for (int i = 0; i < totProps; i++)
                        {
                            //get the drawableIDprops
                            drawableIDprops.Add(Function.Call<int>(Hash.GET_PED_PROP_INDEX, Game.Player.Character, propNumbers[i]));
                            //get the textureIDprops
                            texturesIDprops.Add(Function.Call<int>(Hash.GET_PED_PROP_TEXTURE_INDEX, Game.Player.Character, propNumbers[i]));
                        }
                        //getting all the originally carried weapons
                        CarriedWeapons.Clear();
                        foreach (var weapon in Weapons) //loop through our list of weapons
                        {
                            if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, Game.Player.Character, weapon, false) == true)
                            {
                                CarriedWeapons.Add(weapon);
                                CarriedAmmo.Add(Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON, Game.Player.Character, weapon));
                            }
                        }
                        //get current weapon
                        CurrentWeapon = Function.Call<UInt32>(Hash.GET_SELECTED_PED_WEAPON, Game.Player.Character);
                        //is the weapon equipped
                        CurrentWeaponEquipped = Function.Call<bool>(Hash.IS_PED_ARMED, Game.Player.Character, 1 | 2 | 4);
                    }
                    backToOriginal = false;
                    isScenePlayed = false;
                    //force camera view to 3rd person perspective 
                    Function.Call(Hash.SET_FOLLOW_PED_CAM_VIEW_MODE, 1);
                    //initiate switch to Ped scene
                    switchToNextCharacter();
                }
            }
        }
        //switch to NPC scene/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void switchToNextCharacter()
        {
            modON = true;
            //Stop previous tasks
            Game.Player.Character.Task.ClearAllImmediately();
            //Move the index to the next location
            index++;
            if (index >= myScenes.Count) index = 0;
            //Create the ped to switch to
            newPed = World.CreatePed(myScenes[index].ModelID, myScenes[index].SwitchLocation);
            //Native function to initiate the switch Function.Call(Hash.START_PLAYER_SWITCH, fromPed.Handle, toPed.Handle, flags, switchType);
            Function.Call(Hash.START_PLAYER_SWITCH, Game.Player.Character.Handle, newPed.Handle, 0, 0);
        }
        //switch to original setup////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void switchToOriginalSetup()
        {
            //set player visible
            Function.Call(Hash.SET_ENTITY_VISIBLE, Game.Player.Character, true, 0);
            //Create the ped to switch to
            newPed = World.CreatePed(originalModel, originalLocation);
            //Native function to initiate the switch Function.Call(Hash.START_PLAYER_SWITCH, fromPed.Handle, toPed.Handle, flags, switchType);
            Function.Call(Hash.START_PLAYER_SWITCH, Game.Player.Character.Handle, newPed.Handle, 8, 0);
            //destroy custom camera
            World.RenderingCamera = null;
        }
    }
}