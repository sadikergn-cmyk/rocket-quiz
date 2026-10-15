using System.Collections;
using UnityEngine;

// Owns the rocket's stages, plumes and particle effects.
public class RocketVisual : MonoBehaviour
{
    private static readonly int ThrottleId = Shader.PropertyToID("_Throttle");
    private static readonly int VacuumId = Shader.PropertyToID("_Vacuum");
    private static readonly int EngineLightPosId = Shader.PropertyToID("_EngineLightPos");
    private static readonly int EngineLightColorId = Shader.PropertyToID("_EngineLightColor");
    private static readonly int FrostId = Shader.PropertyToID("_RocketFrost");
    private static readonly int GlowId = Shader.PropertyToID("_Glow");

    [SerializeField] private Transform stage1;
    [SerializeField] private Transform stage2;
    [SerializeField] private Transform fairingLeft;
    [SerializeField] private Transform fairingRight;
    [SerializeField] private Transform probe;
    [SerializeField] private Transform[] solarPanels;
    [SerializeField] private Renderer exhaustStage1;
    [SerializeField] private Renderer exhaustStage2;
    [SerializeField] private Renderer exhaustProbe;
    [SerializeField] private ParticleSystem trailSmoke;
    [SerializeField] private ParticleSystem padSmoke;
    [SerializeField] private ParticleSystem speedStreaks;
    [SerializeField] private Light engineLight;
    [SerializeField] private Transform[] gridFins;
    [SerializeField] private Transform[] legs;
    [SerializeField] private Transform[] engineBells;
    [SerializeField] private Renderer vacuumBell;

    private struct Pose
    {
        public Transform Transform;
        public Transform Parent;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    private Pose[] restPoses;
    private MaterialPropertyBlock block;
    private Vector3[] exhaustScales;
    private MaterialPropertyBlock glowBlock;
    private Quaternion[] bellRest;
    private float throttle;
    private float vacuum;
    private int stage;
    private float frost = 1f;
    private float bellGlow;

    // 0 = full stack, 1 = first stage dropped, 2 = probe only.
    public int Stage
    {
        get { return stage; }
    }

    public bool FairingAttached { get; private set; }

    // Local point the camera should frame, and how far back it needs to sit.
    public Vector3 FocusPoint
    {
        get
        {
            if (stage == 0) return new Vector3(0f, 36f, 0f);
            if (stage == 1) return new Vector3(0f, 57f, 0f);
            return new Vector3(0f, 62.6f, 0f);
        }
    }

    public float FocusSize
    {
        get
        {
            if (stage == 0) return 1f;
            if (stage == 1) return 0.5f;
            return 0.2f;
        }
    }

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        glowBlock = new MaterialPropertyBlock();
        bellRest = new Quaternion[engineBells.Length];
        for (int i = 0; i < engineBells.Length; i++)
        {
            bellRest[i] = engineBells[i].localRotation;
        }
        Transform[] parts = { stage1, stage2, fairingLeft, fairingRight, probe };
        restPoses = new Pose[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            restPoses[i] = new Pose
            {
                Transform = parts[i],
                Parent = parts[i].parent,
                Position = parts[i].localPosition,
                Rotation = parts[i].localRotation,
                Scale = parts[i].localScale,
            };
        }

        Renderer[] exhausts = { exhaustStage1, exhaustStage2, exhaustProbe };
        exhaustScales = new Vector3[exhausts.Length];
        for (int i = 0; i < exhausts.Length; i++)
        {
            exhaustScales[i] = exhausts[i].transform.localScale;
        }

        ResetToPad();
    }

    public void ResetToPad()
    {
        StopAllCoroutines();
        for (int i = 0; i < restPoses.Length; i++)
        {
            Pose pose = restPoses[i];
            pose.Transform.gameObject.SetActive(true);
            pose.Transform.SetParent(pose.Parent, false);
            pose.Transform.localPosition = pose.Position;
            pose.Transform.localRotation = pose.Rotation;
            pose.Transform.localScale = pose.Scale;
        }
        for (int i = 0; i < solarPanels.Length; i++)
        {
            solarPanels[i].localScale = new Vector3(0f, 1f, 1f);
        }

        for (int i = 0; i < gridFins.Length; i++)
        {
            gridFins[i].localRotation = Quaternion.identity;
        }
        for (int i = 0; i < engineBells.Length; i++)
        {
            engineBells[i].localRotation = bellRest[i];
        }

        stage = 0;
        frost = 1f;
        bellGlow = 0f;
        Shader.SetGlobalFloat(FrostId, frost);
        ApplyBellGlow();
        FairingAttached = true;
        SetThrottle(0f);
        SetVacuum(0f);
        SetSmoke(0f);
        SetStreaks(0f);
        trailSmoke.Clear();
        padSmoke.Clear();
        speedStreaks.Clear();
    }

    public void SetThrottle(float value)
    {
        throttle = Mathf.Clamp01(value);
        ApplyExhaust();
    }

    public void SetVacuum(float value)
    {
        vacuum = Mathf.Clamp01(value);
        ApplyExhaust();
    }

    public void SetSmoke(float rate)
    {
        ParticleSystem.EmissionModule emission = trailSmoke.emission;
        emission.rateOverTime = rate;
    }

    public void SetStreaks(float rate)
    {
        ParticleSystem.EmissionModule emission = speedStreaks.emission;
        emission.rateOverTime = rate;
    }

    public void BurstPadSmoke()
    {
        padSmoke.Play();
    }

    public void SeparateStage1()
    {
        if (stage != 0) return;
        stage = 1;
        StartCoroutine(DriftAway(stage1, new Vector3(0.6f, -22f, -0.4f), new Vector3(7f, 0f, 4f), 5f));
        StartCoroutine(DeployGridFins());
        ApplyExhaust();
    }

    public void JettisonFairing()
    {
        if (!FairingAttached) return;
        FairingAttached = false;
        StartCoroutine(DriftAway(fairingLeft, new Vector3(-9f, -10f, 0f), new Vector3(0f, 0f, 55f), 4f));
        StartCoroutine(DriftAway(fairingRight, new Vector3(9f, -10f, 0f), new Vector3(0f, 0f, -55f), 4f));
        StartCoroutine(DeployPanels());
    }

    public void SeparateStage2()
    {
        if (stage != 1) return;
        if (FairingAttached) JettisonFairing();
        stage = 2;
        StartCoroutine(DriftAway(stage2, new Vector3(-0.5f, -14f, 0.3f), new Vector3(-5f, 3f, 0f), 5f));
        ApplyExhaust();
    }

    // Jumps straight to a staging state (skip / reduced motion): dropped parts vanish, no drift.
    public void SnapToStage(int targetStage, bool fairingOff)
    {
        StopAllCoroutines();
        if (targetStage >= 1) stage1.gameObject.SetActive(false);
        if (targetStage >= 2) stage2.gameObject.SetActive(false);
        if (fairingOff || targetStage >= 2)
        {
            fairingLeft.gameObject.SetActive(false);
            fairingRight.gameObject.SetActive(false);
            FairingAttached = false;
            for (int i = 0; i < solarPanels.Length; i++)
            {
                solarPanels[i].localScale = Vector3.one;
            }
        }
        stage = targetStage;
        ApplyExhaust();
    }

    private void ApplyExhaust()
    {
        if (block == null) return;
        Renderer[] exhausts = { exhaustStage1, exhaustStage2, exhaustProbe };
        for (int i = 0; i < exhausts.Length; i++)
        {
            bool active = i == stage && throttle > 0.001f;
            exhausts[i].enabled = active;
            if (!active) continue;

            block.SetFloat(ThrottleId, throttle);
            block.SetFloat(VacuumId, vacuum);
            exhausts[i].SetPropertyBlock(block);

            // Plumes balloon outwards as ambient pressure drops.
            Vector3 rest = exhaustScales[i];
            float spread = Mathf.Lerp(1f, 3.4f, vacuum);
            float length = Mathf.Lerp(0.6f, 1f, throttle) * Mathf.Lerp(1f, 1.5f, vacuum);
            exhausts[i].transform.localScale = new Vector3(rest.x * spread, rest.y * length, rest.z * spread);
        }

        if (engineLight != null)
        {
            // The glow only matters near the ground, where it lights the pad and the airframe.
            float glow = stage == 0 ? throttle * Mathf.Lerp(5f, 0.6f, vacuum) : 0f;
            Vector3 position = engineLight.transform.position;
            Shader.SetGlobalVector(EngineLightPosId, new Vector4(position.x, position.y, position.z, engineLight.range));
            Shader.SetGlobalColor(EngineLightColorId, engineLight.color * glow);
        }
    }

    private void Update()
    {
        bool firing = throttle > 0.001f;

        // Frost sheds once the engines light and the vehicle starts moving through warm air.
        float frostTarget = firing ? 0.2f : 1f;
        float newFrost = Mathf.MoveTowards(frost, frostTarget, Time.deltaTime * (firing ? 0.12f : 1f));
        if (!Mathf.Approximately(newFrost, frost))
        {
            frost = newFrost;
            Shader.SetGlobalFloat(FrostId, frost);
        }

        // Thrust vector control: a few tenths of a degree of restless gimbal while firing.
        if (stage == 0 && firing)
        {
            float t = Time.time;
            for (int i = 0; i < engineBells.Length; i++)
            {
                float x = (Mathf.PerlinNoise(t * 0.7f, i * 3.1f) - 0.5f) * 2.4f;
                float z = (Mathf.PerlinNoise(i * 5.3f, t * 0.7f) - 0.5f) * 2.4f;
                engineBells[i].localRotation = bellRest[i] * Quaternion.Euler(x, 0f, z);
            }
        }

        // The vacuum bell's niobium extension glows orange once it has been firing for a while.
        float glowTarget = stage == 1 && firing ? 0.85f * throttle : 0f;
        float newGlow = Mathf.MoveTowards(bellGlow, glowTarget, Time.deltaTime * 0.35f);
        if (!Mathf.Approximately(newGlow, bellGlow))
        {
            bellGlow = newGlow;
            ApplyBellGlow();
        }
    }

    private void ApplyBellGlow()
    {
        if (vacuumBell == null || glowBlock == null) return;
        glowBlock.SetFloat(GlowId, bellGlow);
        vacuumBell.SetPropertyBlock(glowBlock);
    }

    private IEnumerator DeployGridFins()
    {
        yield return new WaitForSeconds(0.8f);
        float elapsed = 0f;
        const float duration = 1.1f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            for (int i = 0; i < gridFins.Length; i++)
            {
                gridFins[i].localRotation = Quaternion.Euler(90f * t, 0f, 0f);
            }
            yield return null;
        }
    }

    private IEnumerator DriftAway(Transform part, Vector3 velocity, Vector3 spin, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            velocity += new Vector3(0f, -6f, 0f) * dt;
            part.localPosition += velocity * dt;
            part.localRotation *= Quaternion.Euler(spin * dt);
            yield return null;
        }
        part.gameObject.SetActive(false);
    }

    private IEnumerator DeployPanels()
    {
        yield return new WaitForSeconds(0.6f);
        float elapsed = 0f;
        const float duration = 0.9f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // Ease-out-back so the panels snap open with a small overshoot.
            float s = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            for (int i = 0; i < solarPanels.Length; i++)
            {
                solarPanels[i].localScale = new Vector3(s, 1f, 1f);
            }
            yield return null;
        }
    }
}
