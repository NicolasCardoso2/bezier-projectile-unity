<div align="center">

# BezierProjectile — Sistema de Projétil com Curvas de Bézier

**Projétil inteligente para Unity com trajetória matemática, homing dinâmico, chain lightning e object pooling.**

[![Unity](https://img.shields.io/badge/Unity_2021.3+-000000?style=flat-square&logo=unity&logoColor=white)](https://unity.com/)
[![C#](https://img.shields.io/badge/C%23-239120?style=flat-square&logo=csharp&logoColor=white)](https://learn.microsoft.com/en-us/dotnet/csharp/)
[![.NET Standard](https://img.shields.io/badge/.NET_Standard_2.1-512BD4?style=flat-square&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)

> Sistema de projéteis para Unity cujo movimento é calculado frame a frame por equações matemáticas de Curvas de Bézier Quadrática e Cúbica. Implementação do zero das equações, com homing dinâmico, previsão de trajetória, Object Pool e arquitetura desacoplada via ScriptableObject e interfaces.

</div>

---

## Índice

- [Funcionalidades](#funcionalidades)
- [Tecnologias e Requisitos](#tecnologias-e-requisitos)
- [Instalação](#instalação)
- [Como Usar](#como-usar)
- [Referência de Scripts](#referência-de-scripts)
- [Matemática das Curvas](#matemática-das-curvas)
- [Arquitetura](#arquitetura)
- [Estrutura do Projeto](#estrutura-do-projeto)

---

## Funcionalidades

| Funcionalidade | Descrição |
|---|---|
| **Bézier Quadrática** | 1 ponto de controle — arco suave de origem ao alvo |
| **Bézier Cúbica** | 2 pontos de controle — curva em "S" mais expressiva |
| **Homing Dinâmico** | Recalcula a curva a cada frame para rastrear alvos em movimento |
| **Leading (Previsão)** | Antecipa onde o alvo estará com base em sua velocidade atual |
| **Auto-Targeting** | Encontra o inimigo mais próximo automaticamente via `OverlapSphere` |
| **Chain Lightning** | Encadeia para o próximo alvo após impacto com redução de dano por nível |
| **Colisão Real** | `OnTriggerEnter` com filtro por `LayerMask` para colisões físicas |
| **Timeout** | Projétil se autodestrói e dispara evento após tempo limite |
| **Object Pooling** | Elimina `Instantiate`/`Destroy` — reutiliza instâncias pré-criadas |
| **AnimationCurve de Velocidade** | Easing de velocidade totalmente customizável no Inspector |
| **TrailRenderer** | Rastro configurável por cor no `ProjectileData` |
| **Gizmos de Debug** | Visualiza curva, pontos de controle e raio de chain no Scene View |
| **UnityEvents** | `onLaunch`, `onImpact`, `onTimeout` conectáveis no Inspector |

---

## Tecnologias e Requisitos

| Requisito | Versão |
|---|---|
| Unity | 2021.3 LTS ou superior |
| Render Pipeline | Built-in, URP ou HDRP |
| .NET Standard | 2.1 |

> Não há dependências externas de pacotes.

---

## Instalação

1. Clone o repositório:
   ```bash
   git clone https://github.com/NicolasCardoso2/bezier-projectile-unity.git
   ```
2. Copie a pasta `Scripts/` para `Assets/` no seu projeto Unity.
3. Configure o prefab do projétil:
   - Adicione um `GameObject` com os componentes:
     - `BezierProjectile`
     - `Rigidbody` (Is Kinematic ✓, Use Gravity ✗)
     - `Collider` (Is Trigger ✓)
     - `TrailRenderer` *(opcional)*
4. Crie um asset `ProjectileData` via **Assets > Create > Projectile > Data**.
5. Adicione `BezierProjectilePool` em um `GameObject` vazio na cena e atribua o prefab.

---

## Como Usar

### Disparo com alvo definido

```csharp
BezierProjectile proj = BezierProjectilePool.Instance.Get();
proj.data               = meuProjectileData;   // ScriptableObject
proj.transform.position = transform.position;  // posição do canhão
proj.Launch(enemyTransform);
```

### Auto-targeting (sem alvo pré-definido)

```csharp
BezierProjectile proj = BezierProjectilePool.Instance.Get();
proj.data               = meuProjectileData;
proj.transform.position = transform.position;
proj.LaunchAutoTarget(); // busca o IDamageable mais próximo
```

### Implementando IDamageable em um inimigo

```csharp
public class Enemy : MonoBehaviour, IDamageable
{
    public float health = 100f;

    public void TakeDamage(float amount, GameObject source)
    {
        health -= amount;
        if (health <= 0f) Die();
    }

    void Die() => Destroy(gameObject);
}
```

### Reagindo a eventos no Inspector

| Evento | Quando é disparado |
|---|---|
| `onLaunch` | Ao chamar `Launch()` ou `LaunchAutoTarget()` |
| `onImpact` | Ao atingir o alvo ou colidir |
| `onTimeout` | Ao expirar sem atingir o alvo |

---

## Referência de Scripts

### `BezierProjectile`

Componente principal. Geometria de voo, homing, chain e colisão.

| Método | Descrição |
|---|---|
| `Launch(Transform, int)` | Lança em direção a um Transform específico |
| `LaunchAutoTarget(int)` | Busca e lança em direção ao IDamageable mais próximo |
| `FindNearestDamageable(...)` | Utilitário estático de busca por OverlapSphere |

### `ProjectileData` *(ScriptableObject)*

Todos os parâmetros de um tipo de projétil em um único asset reutilizável.

| Campo | Tipo | Descrição |
|---|---|---|
| `curveType` | Enum | `Quadratic` ou `Cubic` |
| `arcHeight` | float | Altura do arco principal |
| `cubicSideOffset` | float | Desvio lateral do 2º ponto de controle (Cubic) |
| `speed` | float | Velocidade base de progressão |
| `speedCurve` | AnimationCurve | Easing da velocidade ao longo de t |
| `timeout` | float | Segundos até expiração forçada |
| `homing` | bool | Rastreia alvos em movimento |
| `leadTarget` | bool | Antecipa posição futura do alvo |
| `autoTargetRadius` | float | Raio de busca automática |
| `damage` | float | Dano base aplicado via IDamageable |
| `chainCount` | int | Profundidade máxima de chain |
| `chainRadius` | float | Raio de busca para o próximo alvo de chain |
| `chainDamageMultiplier` | float | Fator de redução de dano por nível |
| `impactVFXPrefab` | GameObject | Prefab instantiado no impacto |
| `trailStartColor` | Color | Cor inicial do TrailRenderer |
| `trailEndColor` | Color | Cor final do TrailRenderer |

### `BezierProjectilePool`

Singleton de Object Pool. Acesse via `BezierProjectilePool.Instance`.

| Método | Descrição |
|---|---|
| `Get()` | Retira um projétil ativo do pool |
| `Return(BezierProjectile)` | Devolve ao pool (chamado automaticamente) |

### `IDamageable`

Interface implementada por qualquer objeto que pode receber dano.

```csharp
public interface IDamageable
{
    void TakeDamage(float amount, GameObject source);
}
```

---

## Matemática das Curvas

### Bézier Quadrática

$$B(t) = (1-t)^2 P_0 + 2(1-t)t \, P_1 + t^2 P_2$$

**Derivada (tangente / orientação):**

$$B'(t) = 2(1-t)(P_1 - P_0) + 2t(P_2 - P_1)$$

### Bézier Cúbica

$$B(t) = (1-t)^3 P_0 + 3(1-t)^2 t \, P_1 + 3(1-t)t^2 P_2 + t^3 P_3$$

**Derivada (tangente / orientação):**

$$B'(t) = 3(1-t)^2(P_1-P_0) + 6(1-t)t(P_2-P_1) + 3t^2(P_3-P_2)$$

Onde $t \in [0,1]$, $P_0$ = origem, $P_n$ = alvo.

---

## Arquitetura

```
ProjectileData (ScriptableObject)
       │  configurações
       ▼
BezierProjectilePool  ──── Get() / Return() ────▶  BezierProjectile
       │ pré-cria instâncias                               │
       │                                              implementa
       │                                           IDamageable
       │                                       (Enemy, Boss, etc.)
       └── Singleton (DontDestroyOnLoad)
```

**Princípios aplicados:**
- **Single Responsibility**: cada script tem uma única responsabilidade
- **Open/Closed**: adicionar novos tipos de projétil = criar novo `ProjectileData`, sem alterar código
- **Dependency Inversion**: `BezierProjectile` depende de `IDamageable` (abstração), não de `Enemy` (concretização)

---

## Estrutura do Projeto

```
BezierProjectile/
├── Scripts/
│   ├── Core/
│   │   └── BezierProjectile.cs       # Lógica principal do projétil
│   ├── Data/
│   │   └── ProjectileData.cs         # ScriptableObject com todas as configs
│   ├── Interfaces/
│   │   └── IDamageable.cs            # Contrato de dano desacoplado
│   └── Pool/
│       └── BezierProjectilePool.cs   # Object Pool singleton
├── Prefabs/                          # Prefabs prontos para uso
├── ScriptableObjects/                # Assets ProjectileData
├── Demo/                             # Cena de demonstração
├── .gitignore
├── CHANGELOG.md
└── README.md
```

---

<div align="center">

Feito por [Nicolas Cardoso](https://github.com/NicolasCardoso2) · [LinkedIn](https://www.linkedin.com/in/nicolas-cardoso-vilha-do-lago-2483b1322/)

</div>
