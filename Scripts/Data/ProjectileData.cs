using UnityEngine;

/// <summary>Tipo de curva Bézier utilizada pelo projétil.</summary>
public enum BezierCurveType { Quadratic, Cubic }

/// <summary>
/// ScriptableObject com todas as configurações de um tipo de projétil.
/// Um único asset é reutilizável por múltiplos prefabs sem duplicar código.
///
/// Crie via: Assets > Create > Projectile > Data
/// </summary>
[CreateAssetMenu(fileName = "NewProjectileData", menuName = "Projectile/Data")]
public class ProjectileData : ScriptableObject
{
    // -------------------------------------------------------------------------
    // Curva
    // -------------------------------------------------------------------------

    [Header("Curva")]
    [Tooltip("Quadratic = 1 ponto de controle  |  Cubic = 2 pontos de controle (curva em S).")]
    public BezierCurveType curveType = BezierCurveType.Quadratic;

    [Tooltip("Altura do arco gerado pelo ponto de controle principal.")]
    [Min(0f)] public float arcHeight = 5f;

    [Tooltip("Desvio lateral do 2º ponto de controle (somente modo Cubic).")]
    public float cubicSideOffset = 3f;

    // -------------------------------------------------------------------------
    // Movimento
    // -------------------------------------------------------------------------

    [Header("Movimento")]
    [Tooltip("Velocidade base de progressão ao longo da curva (unidades de t por segundo).")]
    [Min(0.01f)] public float speed = 0.5f;

    [Tooltip("Curva de easing: Eixo X = progresso t (0–1), Eixo Y = multiplicador de velocidade.")]
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0.5f, 1f, 1.5f);

    [Tooltip("Segundos até o projétil ser descartado sem ter atingido o alvo.")]
    [Min(0.1f)] public float timeout = 10f;

    // -------------------------------------------------------------------------
    // Homing
    // -------------------------------------------------------------------------

    [Header("Homing")]
    [Tooltip("Recalcula a curva a cada frame para acompanhar um alvo em movimento.")]
    public bool homing = false;

    [Tooltip("Antecipa a posição futura do alvo com base em sua velocidade (requer Homing ativo).")]
    public bool leadTarget = false;

    // -------------------------------------------------------------------------
    // Auto-Targeting
    // -------------------------------------------------------------------------

    [Header("Auto-Targeting")]
    [Tooltip("Raio de busca automática por IDamageable ao chamar LaunchAutoTarget().")]
    [Min(0f)] public float autoTargetRadius = 20f;

    [Tooltip("Máscara de camadas consideradas na busca de alvos.")]
    public LayerMask autoTargetMask = ~0;

    // -------------------------------------------------------------------------
    // Dano e Colisão
    // -------------------------------------------------------------------------

    [Header("Dano e Colisão")]
    [Tooltip("Dano base aplicado ao IDamageable atingido.")]
    [Min(0f)] public float damage = 10f;

    [Tooltip("Camadas com as quais o projétil colide via OnTriggerEnter.")]
    public LayerMask collisionMask = ~0;

    // -------------------------------------------------------------------------
    // Chain Lightning
    // -------------------------------------------------------------------------

    [Header("Chain Lightning")]
    [Tooltip("Profundidade máxima de encadeamento após o impacto. 0 = sem chain.")]
    [Min(0)] public int chainCount = 0;

    [Tooltip("Raio de busca do próximo alvo ao encadear.")]
    [Min(0f)] public float chainRadius = 8f;

    [Tooltip("Fator de redução de dano por nível de chain (0 = sem dano, 1 = dano total).")]
    [Range(0f, 1f)] public float chainDamageMultiplier = 0.5f;

    // -------------------------------------------------------------------------
    // Visual
    // -------------------------------------------------------------------------

    [Header("Visual")]
    [Tooltip("Prefab instantiado no ponto de impacto (partículas, explosão, etc.).")]
    public GameObject impactVFXPrefab;

    [Tooltip("Cor inicial do TrailRenderer (requer componente no prefab).")]
    public Color trailStartColor = Color.white;

    [Tooltip("Cor final do TrailRenderer (transparente = fade out natural).")]
    public Color trailEndColor = new Color(1f, 1f, 1f, 0f);
}
