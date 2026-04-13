# Changelog

Todas as mudanças notáveis neste projeto são documentadas aqui.  
Formato baseado em [Keep a Changelog](https://keepachangelog.com/pt-BR/1.0.0/).

---

## [Unreleased]

> Nenhuma mudança pendente no momento.

---

## [1.2.0] — 2026-04-13

### Adicionado
- `IDamageable` — interface desacoplada para sistemas de dano
- `ProjectileData` — ScriptableObject com todas as configurações reutilizáveis
- `BezierProjectilePool` — Object Pool singleton com pré-aquecimento configurável
- Colisão real via `OnTriggerEnter` com filtro por `LayerMask`
- **Leading**: previsão da posição futura do alvo com base em sua velocidade rastreada
- **Auto-Targeting**: `Physics.OverlapSphere` busca o `IDamageable` mais próximo automaticamente
- **Chain Lightning**: encadeamento de projéteis com redução de dano por nível
- Timeout configurável com `UnityEvent onTimeout`
- `TrailRenderer` configurável por cor via `ProjectileData`
- Lógica de voo migrada de `Update` para `IEnumerator` (Coroutine) para melhor controle de ciclo de vida

### Alterado
- `BezierProjectile` agora consome `ProjectileData` em vez de campos inline
- `Destroy(gameObject)` substituído por `Pool.Return()` para reuso de instâncias

---

## [1.1.0] — 2026-04-13

### Adicionado
- Suporte à **Bézier Cúbica** com dois pontos de controle (curva em "S")
- Enum `CurveType` exposto no Inspector para alternar entre modos
- **Homing dinâmico**: recalculo dos pontos de controle a cada frame
- `AnimationCurve` para easing de velocidade editável no Inspector
- `UnityEvent onLaunch` e `onImpact` para integração no Inspector sem código
- `Mathf.Clamp01` para prevenir overshoot de `t`
- Orientação do projétil via tangente da curva (`Quaternion.LookRotation`)
- Gizmos com handles dos pontos de controle no Scene View

---

## [1.0.0] — 2026-04-13

### Adicionado
- Implementação inicial de `BezierProjectile`
- Bézier Quadrática: `B(t) = (1-t)² · P0 + 2(1-t)t · P1 + t² · P2`
- Campo `arcHeight` para controlar a altura do arco
- Gizmos básicos da curva
