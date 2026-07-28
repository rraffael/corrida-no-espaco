# Créditos de arte

Os sprites, materiais e prefabs de partículas desta pasta vieram do pacote
**Space Shooter Template FREE**, da Unity Asset Store, usado sob a licença padrão
da loja (uso permitido em jogos publicados).

O restante do pacote — scripts, prefabs de gameplay, cena de demonstração e sprites
de shoot-'em-up — foi removido do projeto. Nenhum código do template permanece aqui.

## O que ficou

| Pasta | Conteúdo |
| --- | --- |
| `Background/` | Camadas de nebulosa e de estrelas, para o parallax de fundo |
| `Planets/` | 4 planetas, para passar ao fundo durante a corrida |
| `Ships/` | Sprite da nave/foguete do jogador |
| `VFX/Engines/` | Texturas e materiais do fogo do motor |
| `VFX/Explosions/` | Texturas e materiais de explosão e fumaça |
| `VFX/Prefabs/` | Sistemas de partículas prontos (ver abaixo) |

## Prefabs de partículas

Renomeados a partir dos originais. Nenhum deles depende de código do template.

| Prefab | Original | Observação |
| --- | --- | --- |
| `EngineFlame` | `EngineFx` | Só partículas, sem script |
| `EngineTrail` | `PLayer Engine Effect` | Só partículas, sem script |
| `SpeedLines` | `Speed Effect` | Só partículas, sem script |
| `Explosion` | `Player Explosion` | Usa `Assets/Scripts/VFX/SelfDestruct.cs` |

O `Explosion` originalmente dependia do `VisualEffect.cs` do template. Esse script foi
substituído pelo `SelfDestruct.cs`, escrito para este projeto, e o prefab foi reapontado.
