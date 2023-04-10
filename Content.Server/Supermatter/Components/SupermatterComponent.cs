using Content.Server.Atmos;
using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Content.Server.Supermatter.EntitySystems;
using Content.Shared.Whitelist;


namespace Content.Server.Supermatter.Components
{
    [RegisterComponent]
    [Access(typeof(SupermatterSystem))]
    public sealed class SupermatterComponent : Component
    {
        #region SM Base

        [DataField("whitelist")] public EntityWhitelist Whitelist = new();
        public string IdTag = "EmitterBolt";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("power")]
        public float Power;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("matterPower")]
        public float MatterPower;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("matterPowerConversion")]
        public float MatterPowerConversion = 10f;

        public SupermatterSystem.DelamType DelamType;

        /// <summary>
        /// The portion of the gasmix we're on
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("gasEfficiency")]
        public float GasEfficiency = 0.15f;

        /// <summary>
        /// The current mix the SM is on
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)] public GasMixture? Mix;

        /// <summary>
        /// The amount of heat we apply scaled
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("heatThreshold")]
        public float HeatThreshold = 2500f;

        /// <summary>
        /// Current stream of SM audio.
        /// </summary>
        public IPlayingAudioStream? Stream;

        public SupermatterSystem.SuperMatterSound? SmSound;

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("calmSound")]
        public SoundSpecifier CalmSound = new SoundPathSpecifier("/Audio/Machines/Supermatter/calm.ogg");

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("dustSound")]
        public SoundSpecifier DustSound = new SoundPathSpecifier("/Audio/Machines/Supermatter/dust.ogg");

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("delamSound")]
        public SoundSpecifier DelamSound = new SoundPathSpecifier("/Audio/Machines/Supermatter/delamming.ogg");

        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("delamAlarm")]
        public SoundSpecifier DelamAlarm = new SoundPathSpecifier("/Audio/Machines/alarm.ogg");

        #endregion SM Base

        #region SM Calculation

        /// <summary>
        /// Based on co2 percentage, slowly moves between
        /// 0 and 1. We use it to calc the powerloss_inhibitor
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("powerlossdynamicScaling")]
        public float PowerlossDynamicScaling;

        /// <summary>
        /// Affects the amount of damage and minimum point
        /// at which the sm takes heat damage
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("dynamicheatResistance")]
        public float DynamicHeatResistance = 1;

        /// <summary>
        /// Used to increase or lessen the amount of damage the sm takes
        /// from heat based on molar counts.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("moleheatPenalty")]
        public float MoleHeatPenalty = 350f;

        /// <summary>
        /// Higher == more overall power
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("reactionpowerModefier")]
        public float ReactionPowerModefier = 0.55f;

        /// <summary>
        /// Higher == less heat released during reaction
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("thermalreleaseModifier")]
        public float ThermalReleaseModifier = 5f;

        /// <summary>
        /// Higher == less plasma released by reaction
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("plasmareleaseModifier")]
        public float PlasmaReleaseModifier = 750f;

        /// <summary>
        /// Higher == less oxygen released at high temperature/power
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("oxygenreleaseModifier")]
        public float OxygenReleaseModifier = 325f;

        #endregion SM Calculation

        #region SM Timer

        /// <summary>
        /// The point at which we should start sending messeges
        /// about the damage to the engi channels.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("WarningPoint")]
        public float WarningPoint = 50;

        /// <summary>
        /// The point at which we start sending messages to the common channel
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("emergencyPoint")]
        public float EmergencyPoint = 500;

        /// <summary>
        /// we yell if over 50 damage every YellTimer Seconds
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("yellTimer")]
        public float YellTimer = 20f;

        /// <summary>
        /// set to YellTimer at first so it doesnt yell a minute after being hit
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("yellAccumulator")]
        public float YellAccumulator = 20f;

        /// <summary>
        /// YellTimer before the SM is about the delam
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("yellDelam")]
        public float YellDelam = 5f;

        /// <summary>
        ///  Timer for Damage
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("damageupdateAccumulator")]
        public float DamageUpdateAccumulator;

        /// <summary>
        /// update environment damage every 0.5 second
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("damageupdateTimer")]
        public float DamageUpdateTimer = 0.5f;

        /// <summary>
        /// Timer for delam
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("delamtimerAccumulator")]
        public float DelamTimerAccumulator;

        /// <summary>
        /// updates delam
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("delamtimerTimer")]
        public int DelamTimerTimer = 30;

        /// <summary>
        ///  The message timer
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("speakaccumulator")]
        public float SpeakAccumulator = 5f;

        /// <summary>
        /// Atmos update timer
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("atmosupdateAccumulator")]
        public float AtmosUpdateAccumulator;

        /// <summary>
        /// update atmos every 0.5 second
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("atmosupdateTimer")]
        public float AtmosUpdateTimer = 0.5f;

        #endregion SM Timer

        #region SM Threshold

        /// <summary>
        /// Higher == Higher percentage of inhibitor gas needed
        /// before the charge inertia chain reaction effect starts.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("powerlossinhibitiongasThreshold")]
        public float PowerlossInhibitionGasThreshold = 0.20f;

        /// <summary>
        /// Higher == More moles of the gas are needed before the charge
        /// inertia chain reaction effect starts.
        /// Scales powerloss inhibition down until this amount of moles is reached
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("powerlossinhibitionmoleThreshold")]
        public float PowerlossInhibitionMoleThreshold = 20f;

        /// <summary>
        /// bonus powerloss inhibition boost if this amount of moles is reached
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("powerlossinhibitionmoleboostThreshold")]
        public float PowerlossInhibitionMoleBoostThreshold = 500f;

        /// <summary>
        /// Above this value we can get lord singulo and independent mol damage,
        /// below it we can heal damage
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("molepenaltyThreshold")]
        public float MolePenaltyThreshold = 1800f;

        /// <summary>
        /// more moles of gases are harder to heat than fewer,
        /// so let's scale heat damage around them
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("moleheatpenaltyThreshold")]
        public float MoleHeatPenaltyThreshold;

        /// <summary>
        /// The cutoff on power properly doing damage, pulling shit around,
        /// and delamming into a tesla. Low chance of pyro anomalies, +2 bolts of electricity
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("powerPenaltyThreshold")]
        public float PowerPenaltyThreshold = 5000f;

        /// <summary>
        /// Higher == Crystal safe operational temperature is higher.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("heatpenaltyThreshold")]
        public float HeatPenaltyThreshold = 40f;

        /// <summary>
        /// is multiplied by ExplosionPoint to cap
        /// evironmental damage per cycle
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("damageHardcap")]
        public float DamageHardcap = 0.002f;

        /// <summary>
        /// environmental damage is scaled by this
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("damageincreaseMultiplier")]
        public float DamageIncreaseMultiplier = 0.25f;

        /// <summary>
        /// if spaced sm wont take more than 2 damage per cycle
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("maxspaceexposureDamage")]
        public float MaxSpaceExposureDamage = 2;

        #endregion SM Threshold

        #region SM Delamm

        /// <summary>
        /// The point at which we delamm
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("explosionPoint")]
        public int ExplosionPoint = 900;

        //it's the final countdown
        [ViewVariables(VVAccess.ReadOnly)] public bool FinalCountdown = false;

        //Explosion totalIntensity value
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("totalIntensity")]
        public float TotalIntensity= 500000f;

        //Explosion radius value
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("radius")]
        public float Radius = 500f;

        /// <summary>
        /// These would be what you would get at point blank, decreases with distance
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("detonationRads")]
        public float DetonationRads = 200f;

        #endregion SM Delamm

        #region SM Gas
        /// <summary>
        /// Is used to store gas
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("gasStorage")]
        public Dictionary<Gas, float> GasStorage = new Dictionary<Gas, float>()
        {
            {Gas.Oxygen, 0f},
            {Gas.Nitrogen, 0f},
            {Gas.CarbonDioxide, 0f},
            {Gas.Plasma, 0f},
            {Gas.Tritium, 0f},
            {Gas.WaterVapor, 0f}
        };

        /// <summary>
        /// Stores each gases calculation
        /// </summary>
        public readonly Dictionary<Gas, (float TransmitModifier, float HeatPenalty, float PowerMixRatio)> GasDataFields = new()
        {
            [Gas.Oxygen] = (TransmitModifier: 1.5f, HeatPenalty: 1f, PowerMixRatio: 1f),
            [Gas.Nitrogen] = (TransmitModifier: 0f, HeatPenalty: -1.5f, PowerMixRatio: -1f),
            [Gas.CarbonDioxide] = (TransmitModifier: 0f, HeatPenalty: 0.1f, PowerMixRatio: 1f),
            [Gas.Plasma] = (TransmitModifier: 4f, HeatPenalty: 15f, PowerMixRatio: 1f),
            [Gas.Tritium] = (TransmitModifier: 30f, HeatPenalty: 10f, PowerMixRatio: 1f),
            [Gas.WaterVapor] = (TransmitModifier: 2f, HeatPenalty: 12f, PowerMixRatio: 1f)
        };

        #endregion SM Gas

        //TODO:
        #region SM TODO
        /*
        /// <summary>
        /// PsyCoeff should change from 0-1 based on psycologist distance
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("psyCoeff")]
        public float PsyCoeff = 0;

        /// <summary>
        /// Along with damagepenaltypoint, makes flux anomalies.
        /// The cutoff for the minimum amount of power required
        /// to trigger the crystal invasion delamination event.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("eventpowerpenaltyThreshold")]
        public float EventPowerPenaltyThreshold = 4500f;

        /// <summary>
        /// +1 bolt of electricity, allows for gravitational anomalies,
        /// and higher chances of pyro anomalies
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("severepowerpenaltyThreshold")]
        public float SeverePowerPenaltyThreshold = 7000f;

        /// <summary>
        /// +1 bolt of electricity.
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("criticalpowerpenaltyThreshold")]
        public float CriticalPowerPenaltyThreshold = 9000f;

        /// <summary>
        /// Causes hallucination when you look at the SM or when the SM delams
        /// decreases with distance
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("detonationHallucination")]
        public float DetonationHallucination = 600f;

        /// <summary>
        /// When we pass this amount of damage we start shooting bolts
        /// </summary>
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("damagepenaltypoint")]
        public int Damagepenaltypoint = 550;

        */
        #endregion SM TODO

    }
}
