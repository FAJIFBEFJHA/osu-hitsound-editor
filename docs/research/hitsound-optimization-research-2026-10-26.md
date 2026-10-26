# Investigación técnica: optimización de hitsounds para osu!standard

**Proyecto:** osu-hitsound-editor · **Cierre del informe:** 26 de octubre de 2026 (UTC); fuentes consultadas el 25–26 de octubre.
**Naturaleza:** investigación de solo lectura; no es una propuesta aprobada de implementación.
**Compatibilidad canónica:** formato legacy y decisiones vigentes del proyecto para osu!stable.

## 1. Resumen ejecutivo

**El problema es una asignación de recursos con restricciones de resolución y reproducción. No se reduce a eliminar archivos idénticos ni a colorear samples por simultaneidad.** La unidad que debe conservarse es cada reproducción lógica, con su identidad, disparador, tiempo, multiplicidad, volumen y comportamiento continuo o discreto. El nombre físico es una forma de alcanzarla, no su identidad.

Los resultados principales son:

1. **CONFIRMADO:** D002–D005 mantienen identidad lógica separada, equivalencia explícita, prioridad lexicográfica y la prohibición de adelantar un optimizador global al verificador de exportación. El roadmap sigue marcando exportación y optimización como no iniciadas. [P1][P2]
2. **CONFIRMADO:** en el modelo canónico de círculos, un filename explícito produce solo ese sample; no es una quinta ranura que pueda sumarse a Normal/Whistle/Finish/Clap. La fixture histórica del proyecto distingue este resultado del de lazer. [P1][P8][O2]
3. **DEDUCIDO:** cuatro es la capacidad de capas estándar de un disparo ordinario en el modelo con Normal activo, no un límite global de voces por instante. Cinco eventos repartidos entre objetos, ticks y cuerpos pueden coexistir; cinco samples independientes asignados a un único círculo no caben bajo las transformaciones conservadoras de este informe.
4. **CONFIRMADO:** existen muestras universales sin banco, y el índice efectivo 0 evita las búsquedas locales. **DEDUCIDO:** la presencia de un universal puede ahorrar copias entre índices, pero también sustituir silenciosamente un fallback que otra dificultad necesitaba. Se debe resolver el paquete completo, incluidas las consultas que actualmente no encuentran archivo local. [P4][O4][O5]
5. **DEDUCIDO:** las parejas `{A,B}`, `{A,C}` y `{B,C}` requieren al menos cuatro archivos en el submodelo especificado en §4.5, aunque solo haya dos sonidos por objeto. Se aporta una construcción de cuatro y una prueba de cota inferior. No se afirma haber ejecutado esa construcción en stable.
6. **CONFIRMADO:** la arquitectura ya separa cálculo de hitsounds, búsqueda física y reproducción. **CONFIRMADO:** aún no conserva todo lo necesario para una exportación íntegra: faltan identidad lógica persistente, datos de curvas y campos/secciones no interpretados; la enumeración temporal tampoco incluye finales de spinner. Esto exige contratos de conservación, no una división de `Beatmap.cs` por tamaño. [P3][P5][P6][P7]
7. **DEDUCIDO:** la estrategia inicial más defendible es exportación conservadora verificable y, después, heurísticas deterministas con verificación independiente y búsqueda exacta acotada para casos pequeños. CP-SAT es una alternativa futura razonable para un modelo booleano comprobado, no una necesidad demostrada hoy.

La equivalencia audible universal **no está demostrada** por el modelo actual. La configuración `LayeredHitSounds`, la desactivación de samples del beatmap, los mínimos de volumen, la tolerancia alrededor de timing points y la reproducción simultánea de la misma muestra son condiciones materiales. El informe delimita qué se sabe y qué necesita una prueba en stable.

### 1.1 Evidencia, alcance y reproducibilidad

| Fuente | Revisión consultada | Alcance de la evidencia |
|---|---|---|
| Producto | `1c75405d3a131f367e4c9c8be692c6c012d1c7e7` | Código, pruebas y documentación publicados en `main`. |
| Workflow | `49da4c5af05826f71518b38caa303635af1fdcc5` | `PROJECT_PROFILE.md`, `PROJECT_CONTEXT.md`, `AI_WORKFLOW.md`. |
| Implementación oficial `ppy/osu` | `131fe72a15581049401a588a13684938d4d0e0c7` | Código de lazer, importación/exportación legacy, skin y pruebas; no se identifica con el código de stable. |
| Wiki oficial y OR-Tools | Consulta del 25-10-2026 | Documentación vigente consultada; enlaces precisos en §10. |

El SHA del producto coincide con el registrado en `PROJECT_CONTEXT.md`. El resultado **741/741 del 21-10-2026 es evidencia histórica documentada**, no una ejecución realizada para esta investigación. No se ejecutaron tests, builds, clientes osu!, experimentos de audio ni solvers; tampoco se modificó ninguno de los repositorios. Los ejemplos nuevos son especificaciones reproducibles y deducciones revisadas, no resultados experimentales. No se inspeccionaron cambios locales no publicados del usuario.

Se utilizan cuatro etiquetas:

- **CONFIRMADO:** afirmación respaldada por el código, una prueba existente leída, documentación oficial o un resultado histórico registrado. Se especifica cuál de esas fuentes la respalda.
- **DEDUCIDO:** consecuencia razonada de premisas identificadas; una prueba matemática puede confirmar un óptimo del modelo sin probar la fidelidad del modelo al cliente.
- **HIPÓTESIS:** comportamiento o rendimiento que requiere validación.
- **DESCONOCIDO:** las fuentes disponibles no permiten resolverlo.

Los endpoints consultados de `peppy/osu-stable-reference` devolvieron 404. No se atribuye a ese repositorio una inspección directa. Cuando el código oficial de lazer documenta compatibilidad o diferencias con stable, se trata como esa clase de evidencia, y no como una nueva medición del cliente estable.

## 2. Matriz de restricciones

### 2.1 Selección de capas y recursos

Se distingue **valor escrito** de **valor efectivo**: `hitSample.index=0` significa heredar; solo después de resolver la herencia se sabe si el índice efectivo es 0, 1 o mayor.

| ID | Estado y regla | Consecuencia para la optimización | Evidencia precisa |
|---|---|---|---|
| R01 | **CONFIRMADO, modelo actual:** `GetHitSoundTypes` añade Normal y las adiciones de bits 2, 4 y 8. | Como máximo N+W+F+C por disparo ordinario; los bits no representan cantidades. | [P3], líneas 415–432; [O1], Hitsounds. |
| R02 | **CONFIRMADO:** Normal usa `normalSet`; W/F/C comparten `additionSet`. | No se pueden escoger tres bancos independientes para las tres adiciones de un objeto. | [P3], 439–482; [O2], `convertSoundType`. |
| R03 | **CONFIRMADO:** las capas ordinarias de un círculo comparten índice y volumen efectivos. | `A@50% + B@25%` no es exportable como dos capas de ese círculo con el mecanismo ordinario y audio sin modificar. | [P3], 327–378 y 439–482. |
| R04 | **CONFIRMADO:** normalSet 0 hereda el banco temporal; additionSet 0 sigue al Normal resuelto. Índice y volumen escritos como 0 heredan. | Un cero no representa una asignación física libre ni silencio. | [P3], 327–412; [O3], `LegacySampleControlPoint.ApplyTo`. |
| R05 | **CONFIRMADO, D017:** índice efectivo 0 no busca archivos del beatmap; índice 1 sí. | Normalizar 0 y 1 al mismo estado cambia comportamiento aunque ambos omitan sufijo numérico. | [P4], 63–76; [P9], `ResolveSample_DoesNotUseBeatmapSampleWhenSampleIndexIsZero`; [O5]. |
| R06 | **CONFIRMADO, D022:** para índice ≥1, buscar específico del banco/índice, después universal sin banco, después fallback externo. | Las posiciones físicas no son una matriz independiente: un universal afecta todas las consultas elegibles del mismo tipo que carezcan de específico. | [P4], 325–328; [P9], pruebas de universal; [O4], `getLegacyLookupNames`. |
| R07 | **CONFIRMADO:** el índice 1 usa, por ejemplo, `normal-hitclap.wav`; el 2 usa `normal-hitclap2.wav`. El universal es `hitclap.wav`, sin índice. | `hitclap2.wav` no es el universal del índice 2 en el resolver actual. Tampoco se cae desde índice 2 a `normal-hitclap.wav` local. | [P4], 60–76; [O4], 610–633. |
| R08 | **CONFIRMADO en el producto:** cada nombre estándar prueba `.wav`, `.mp3`, `.ogg`, en ese orden; el específico completo se resuelve antes del universal. | Un específico `.ogg` gana a un universal `.wav`. No usar una preferencia global de extensión que mezcle los niveles. | [P4], 235–276 y 325–328; [P9], pruebas de extensión. |
| R09 | **CONFIRMADO, D020 y fixture histórica:** filename explícito en círculo/spinner produce Custom solamente en el modelo legacy elegido. | No permite Custom+adiciones ni listas arbitrarias de archivos. Puede apuntar a un archivo también alcanzado por nombre estándar en otro objeto. | [P3], 442–452; [P8]; [O1], Custom hit samples. |
| R10 | **CONFIRMADO en el producto:** custom usa la ruta exacta; faltar produce `CustomSampleMissing`, distinto de `ExternalFallbackRequired`. | No convertir ambos resultados en silencio ni en un único SampleId. La respuesta final de stable ante un custom ausente no queda demostrada por este enum. | [P4], 207–233 y 279–297; [P1], D017. |
| R11 | **CONFIRMADO:** el fallback estándar conserva el banco/tipo, pero elimina el índice al buscar en skin/default. | Si se pretende equivalencia bajo distintas skins, cambiar tipo o banco puede cambiar el sonido aunque la copia local sea idéntica. | [O1], búsqueda; [P4], `FallbackFilename`. |
| R12 | **CONFIRMADO documentalmente:** `LayeredHitSounds` determina si Normal implícito se añade siempre. | «Normal siempre suena» solo describe el modelo actual/configuración de referencia, no todas las skins. El bit Normal explícito y el Normal implícito no son automáticamente equivalentes. | [O6]; [O2], `IsLayered`; [O7]; [O8]. |
| R13 | **DEDUCIDO:** un mismo archivo puede tener muchos consumidores y muchas voces. | Reutilizar bytes no autoriza eliminar reproducciones repetidas. Un set de SampleId pierde multiplicidad. | R01–R11 y modelo §3. |
| R14 | **DEDUCIDO:** varios archivos con audio idéntico pero distintos nombres siguen siendo varios recursos físicos exportados. | D004 no se cumple contando solo hashes únicos. Enlaces del sistema de archivos no proporcionan alias portables del formato `.osu/.osz`. | D002–D004; nombres de R06–R09. |

### 2.2 Sliders, tiempo y entorno

| ID | Estado y regla | Consecuencia | Evidencia |
|---|---|---|---|
| R15 | **CONFIRMADO, D021:** cada borde tiene `edgeSounds` y `edgeSets`; índice/volumen se obtienen del timing point del tiempo real del borde. | Un slider no dispone de un índice independiente serializado en cada `edgeSet`. Cambiar un timing point afecta otros consumidores. | [P3], 484–526; [P10], `UsesTimingPointAtEdgeTime`; [O2], parse de sliders. |
| R16 | **CONFIRMADO, D021:** trailing NormalSet/AdditionSet participan en cuerpo/ticks; trailing Index/Volume/Filename no son overrides generales de slider. | No usar `...:99:20:custom.wav` como forma de individualizar bordes, ticks o cuerpo. | [P3], 528–565; [P10], pruebas `IgnoresSliderHitSampleIndexVolumeAndFilename`; [O2], `banksOnly`. |
| R17 | **CONFIRMADO, modelo:** cuerpo: SliderSlide más SliderWhistle si procede; ticks: SliderTick del Normal. Cuerpo/ticks toman índice/volumen al inicio. | F/C no añaden loops de cuerpo. Reasignar un tipo entre borde, tick y loop altera la clase de reproducción. | [P3], 528–565; [O9], `UpdateNestedSamples`; [O10], `LoadSamples`. |
| R18 | **CONFIRMADO:** cuerpos son continuos y dependen del seguimiento del slider; las fuentes se repiten. | Deben preservarse intervalo, fase, repetición y condición de actividad, no solo una marca de inicio. | [O1], Slider hitsounds; [O10], actualización de tracking; [P7], `PlaySliderBody`. |
| R19 | **CONFIRMADO en el código:** bordes/ticks usan tiempos `double`, y los ticks dependen de versión, longitud, SV, beatLength y tick rate. | Redondear a milisegundos enteros puede cruzar un punto de herencia o cambiar una superposición. | [P3], 115–298; [P10], pruebas de versiones 7/8/14 y tiempos fraccionarios. |
| R20 | **CONFIRMADO en lazer legacy:** `CONTROL_POINT_LENIENCY=5`; la aplicación de samples consulta bordes con +5 ms y cuerpo con +6 ms; no repetidores usan final+5 ms. | El proyecto consulta `<=time` sin esta tolerancia. La discrepancia merece fixtures; no debe copiarse a stable por suposición. | [O11], 33–39 y 168–188; [P3], 64–74. |
| R21 | **CONFIRMADO en lazer:** mínimo de volumen 5%. Su comentario oficial documenta 8% en stable. El motor del proyecto aplica `volume/100f` directamente. | Igualar solo el número escrito no demuestra igualdad de amplitud entre clientes. Mantener valores durante optimización; caracterizar mínimos antes de compensar. | [O12], 168–186; [P11], 216 y 334. |
| R22 | **CONFIRMADO:** el código oficial posiciona estéreo según posición del objeto; en slider sigue la bola. | No mover sonidos entre objetos/posiciones para ahorrar archivos. Verificar por separado el alcance exacto en stable. | [O12], `CalculateSamplePlaybackBalance`; [O10], `slidingSample.Balance`. |
| R23 | **DEDUCIDO:** dificultades no se reproducen simultáneamente, pero sí comparten el espacio de nombres de la carpeta. | Un archivo no puede contener A en una dificultad y B en otra. La unión de recursos puede crear overrides nuevos. | R06–R11 y distribución legacy documentada. |

### 2.3 Clasificación: restricciones adicionales

**CONFIRMADO, reglas oficiales:** no deben quedar archivos sin uso ni de cero bytes; las partes clicables necesitan feedback audible con impacto, pico retrasado como máximo 5 ms y un sample WAV sin comprimir u Ogg; los hitsounds deben durar al menos 25 ms, con la excepción expresamente indicada para el archivo silencioso oficial de 44 bytes. Un storyboard no sustituye feedback activo. [O13]

**CONFIRMADO, guía oficial:** se desaconseja reutilizar frecuentemente Finish de bancos Normal/Soft para melodías o percusión porque al desactivar samples del beatmap cambia la experiencia. También se desaconseja distorsión injustificada. [O13] En osu!standard, el sonido de una parte activamente clicada debe distinguirse de la canción; una keysound sola puede ser insuficiente. [O14]

**DEDUCIDO:** legalidad sintáctica, equivalencia y posibilidad de clasificación son tres verificaciones distintas. Los ejemplos de señales sintéticas de §4 sirven para reproducir un análisis; no certifican rankabilidad. No se debe añadir un archivo vacío para «ocupar» Normal: archivo ausente, archivo silencioso válido y archivo corrupto no significan lo mismo.

### 2.4 Qué significa «cambiar de canal»

**CONFIRMADO en el motor del proyecto:** `PlayHitSoundLayer` entrega la ruta y el volumen al mismo `AddAudioSource`; no aplica una ganancia distinta por W/F/C/N. Esto permite probar equivalencia dentro de ese motor, pero no prueba por sí solo todos los clientes. [P7][P11]

**DEDUCIDO:** un intercambio de canales solo puede admitirse tras conservar identidad del audio, cantidad de voces, disparador, parámetros de reproducción y todas las resoluciones externas del entorno contratado. Ya existen contraejemplos sin invocar diferencias de ganancia: Normal implícito con `LayeredHitSounds=0`; cambio de Clap a Finish con samples locales desactivados; cambio de golpe a loop; cambio de banco cuyo fallback es distinto.

**DESCONOCIDO:** no se encontró evidencia suficiente en esta investigación para certificar que cualquier reasignación entre los cuatro canales mantiene exactamente volumen, concurrencia y procesamiento audible en todas las versiones/configuraciones de stable. Por tanto, las reasignaciones de tipo deben permanecer fuera de una primera optimización conservadora hasta caracterizarlas. Las pruebas de optimalidad posteriores declaran expresamente un entorno de referencia; no eliminan esta limitación.

## 3. Modelo formal propuesto

### 3.1 Dominio y transformaciones admitidas

**DEDUCIDO / propuesta de contrato, sin cambiar decisiones:** definir el problema sobre un paquete completo de dificultades `D`, archivos y referencias compartidas. Conservar geometría, cantidad de objetos, modo, tiempos, reglas de activación y audio. Permitir solo las representaciones de samples autorizadas por el contrato: nombres, bancos, índices, flags y determinados campos de herencia. No añadir objetos, mezclar previamente muestras, normalizar amplitudes, transcodificar ni trasladar eventos a storyboard.

Hay dos espacios de búsqueda útiles, que no deben confundirse:

- **Conservador:** conserva tipos, flags, bancos y comportamiento externo; explora únicamente cambios cuya equivalencia esté probada. Es el apropiado para empezar.
- **Ampliado bajo entorno declarado:** permite reasignaciones de canales/bancos que hayan pasado la caracterización del cliente. Su óptimo puede ser menor; no se presenta como óptimo del primer espacio ni como equivalente para toda skin.

El entorno `Ω` incluye versión de cliente, skin relevante, samples del beatmap habilitados/deshabilitados, `LayeredHitSounds`, opciones de audio y comportamiento de velocidad. No es necesario implementar ahora una configuración stable/lazer: D020 mantiene un único modelo canónico. Sí es necesario documentar sobre qué entorno se afirma equivalencia.

### 3.2 Identidad lógica y eventos

Sea `S` el conjunto de identidades lógicas. Dos identidades no se fusionan por semejanza perceptual. D003 permite una equivalencia explícita `~`; la optimización trabaja con clases de equivalencia solo cuando el criterio ha sido autorizado y verificado.

Una ocurrencia discreta se representa conceptualmente mediante:

`e = (difficulty, objectId, eventKind, eventOrdinal, sampleId, nominalTime, trigger, volume, playbackParameters)`.

No es una recomendación de clase C#: es la información necesaria para decidir equivalencia. `trigger` distingue pulsación real, borde condicionado, tick, spinner y evento autónomo. El tiempo nominal no reemplaza al tiempo de pulsación. Repeticiones idénticas son ocurrencias distintas: el conjunto de eventos es un **multiconjunto**.

Para el cuerpo de slider se necesitan además intervalo nominal `[start,end)`, regla de activación, periodo/fase del loop y políticas de pausa/reanudación. Estos datos no pueden sustituirse por un solo evento de inicio. Las referencias externas se representan simbólicamente, por ejemplo `External(Soft,Clap,policy)`, y un custom ausente conserva su ruta y estado de error. No se les inventa un hash de audio.

**Identidad de contenido:** para el primer verificador, una copia byte a byte es un criterio fuerte y sencillo. Un hash criptográfico permite indexar candidatos, con comprobación de bytes cuando se requiera igualdad estricta. Igualdad de PCM decodificado puede ser un criterio posterior, especificando decoder, canales, tasa, longitud, priming y metadatos de loop. Igualdad después de resamplear o normalizar no es automáticamente igualdad de audio original. Mantener SampleId de edición separado del fingerprint: cambiar el contenido de una muestra no debería confundirse con renombrarla.

### 3.3 Posiciones físicas y consulta

Sea `P` un conjunto finito de **rutas de salida reales**, no de pares banco/índice. Incluye:

- nombres específicos `(bank,type,index,extension)`;
- nombres universales `(type,extension)` sin índice;
- rutas arbitrarias de custom;
- recursos protegidos ya referenciados por otras partes del paquete.

Una ruta debe ser una sola variable física aunque sea alcanzada por lookup estándar y por filename explícito. Por ejemplo, `normal-hitnormal.wav` puede servir al círculo ordinario y al círculo que lo nombra explícitamente.

Cada consulta `q` tiene una lista ordenada de candidatos `L(q)`. El resultado es el primer archivo elegible, seguido, cuando corresponda, de una referencia externa. Se debe representar la **ausencia** de candidatos de mayor prioridad. No basta afirmar «existe un archivo con A»: un archivo con B en un nivel anterior puede ocultarlo.

Las solicitudes de un objeto están acopladas: un solo índice, un banco normal y un banco de adiciones, una máscara de flags y un volumen. Los sliders añaden dependencias con controles temporales compartidos. Las dificultades comparten `P`, pero no comparten necesariamente sus controles temporales.

### 3.4 Formulación binaria mediante patrones legales

Es una formulación conceptual exacta **dentro de un dominio finito de candidatos declarado**. No requiere escoger solver.

Para cada grupo de disparo `g`, generar alternativas legales `K_g`. Una alternativa fija campos del formato, asigna todas sus ocurrencias a canales permitidos y especifica qué ruta debe ganar cada lookup. Para sliders se pueden usar patrones que abarcan el slider completo o patrones de componentes unidos mediante restricciones sobre los mismos controles temporales.

Cada patrón `k` registra:

- `Req(g,k)`: parejas `(p,s)` que deben existir y contener `s`;
- `Absent(g,k)`: rutas que deben faltar para que gane el nivel correcto;
- `Pairs(g,k)`: parejas efectivas `(bank,index)` realmente consultadas;
- requerimientos de timing, activación, volumen, flags y resultados externos.

Variables:

| Variable | Dominio | Significado |
|---|---|---|
| `x[g,k]` | {0,1} | Se escoge la alternativa k del grupo g. |
| `y[p,s]` | {0,1} | La ruta p contiene la clase de audio s. |
| `u[p]` | {0,1} | Existe un archivo optimizable en p. |
| `z[b,i]` | {0,1} | Se usa al menos una pareja efectiva banco/índice. |
| `r[t,a]` | {0,1}, si se habilita | El control temporal t usa la configuración permitida a. |

Restricciones fundamentales:

1. `Σ(k∈K_g) x[g,k] = 1` para cada grupo.
2. `Σ(s∈S) y[p,s] = u[p] ≤ 1` para cada ruta optimizable. Una ruta no aloja dos contenidos.
3. `x[g,k] ≤ y[p,s]` para cada requisito `(p,s)` del patrón.
4. `x[g,k] + u[p] ≤ 1` para cada ruta que debe estar ausente.
5. `z[b,i] ≥ x[g,k]` cuando el patrón consulta esa pareja. Puede imponerse también la desigualdad inversa contra la suma de patrones que la usan para obtener una equivalencia exacta.
6. Recursos o referencias protegidos se fijan. Una ausencia requerida que contradiga un archivo protegido vuelve inviable el patrón.
7. Para un timing point editable, `Σ_a r[t,a]=1`; un patrón que requiere la opción a impone `x[g,k]≤r[t,a]`. Dos bordes simultáneos no pueden pedir índices incompatibles al mismo punto.
8. Todos los patrones deben preservar **el multiconjunto completo**, incluidos Normal obligatorio, capas externas y condiciones de activación. No se admite cobertura parcial ni sonidos adicionales.

La verificación semántica final es obligatoria incluso si el modelo es satisfacible. Un generador de patrones defectuoso puede producir un óptimo matemático incorrecto para osu!.

**Suficiencia del modelo:** si cada patrón representa realmente una serialización legal, sus requerimientos y ausencias describen todas las búsquedas, y se cumplen los acoplamientos de tiempo/paquete, las restricciones garantizan que cada consulta selecciona el contenido previsto. **Completitud:** solo se puede afirmar si el generador incluye todas las alternativas del dominio anunciado. Recortar índices, tipos o patrones ofrece optimalidad restringida, no global.

### 3.5 Función objetivo de D004

Sean `F = Σ_p u[p]` y `C = Σ_(b,i) z[b,i]`. Con un conjunto fijo de U clases de audio locales requeridas y sin audios nuevos, la redundancia es:

`R = Σ_s (copies(s) − 1) = F − U`.

Minimizar `R` equivale a minimizar `F` bajo esas premisas. Si se permiten muestras auxiliares, materialización de fallbacks, mezclas o eliminación de sonidos silenciosos, U deja de ser fijo y esta equivalencia necesita otra definición: no se debe aprovechar ese cambio para aparentar ahorro.

Objetivo:

`preservar equivalencia como restricción; después minimizar lexicográficamente (R, C)`.

Resolver primero R; fijar su óptimo demostrado; resolver después C. Si R no se ha demostrado óptimo, informar el vector de la mejor solución válida y su cota, sin afirmar que C es el mínimo entre todos los mínimos de R. Una suma ponderada solo sería equivalente con una cota probada para el objetivo secundario; no usar pesos arbitrarios.

**Propuesta de definición operativa pendiente de ratificación:** C cuenta la unión de parejas efectivas de capas estándar, cuerpo y ticks en todo el paquete, incluyendo índices efectivos 0 cuando exista consulta externa; custom-only no añade una pareja sin uso. Si Normal y adiciones usan bancos distintos cuentan dos parejas. El universal reduce rutas, no necesariamente C. Informar también C por dificultad ayuda a explicar el resultado. D004 no especifica todavía todas estas convenciones.

**Tamaño:** registrar `B = Σ_p bytes(p)` y, separadamente, tamaño del `.osz`. Es una métrica complementaria útil: duplicar un sample de 5 MB cuesta más que duplicar uno de 5 KB, aunque ambos sumen una copia. No anteponer B a C. Como máximo, proponer B como tercer desempate tras aprobación; inicialmente basta reportarlo. Compresión ZIP, tamaño instalado, memoria decodificada y número de archivos son métricas distintas.

### 3.6 Correspondencias con otros problemas

| Familia | Correspondencia justificada | Límite de la analogía |
|---|---|---|
| Satisfacción/asignación con restricciones | Variables físicas finitas, alternativas por evento y restricciones compartidas. | Es la descripción general más fiel; no implica un solver particular. |
| Coloreo de grafos | Si cada sample tiene un único canal fijo, dos samples que coocurren deben ocupar canales distintos. Para parejas con Normal obligatorio aparece además una partición Normal/adición. | Colores no representan índices completos ni costos de copias; falta precedencia, custom, volumen y herencia. No se demuestra aquí una reducción del problema completo a coloreo. |
| Cobertura con costo compartido | Varias alternativas pueden reutilizar el mismo archivo y pagar su costo una sola vez. | La cobertura ordinaria es insuficiente: sobrar una capa invalida la solución y añadir un universal puede romper otra consulta. La factibilidad no es monótona al añadir archivos. |
| Programación entera / CP | El modelo binario de §3.4 expresa selección, ocupación, ausencia y dependencias. | Su expresabilidad no demuestra que CP-SAT sea necesario ni que una formulación particular escale bien. [A1][A2] |

No se afirma NP-completitud del problema completo sin una reducción formal. Una búsqueda exhaustiva de este modelo tiene crecimiento combinatorio; eso sí se puede establecer directamente contando sus alternativas.

## 4. Casos de estudio reproducibles y contraejemplos

### Convenciones y condiciones

Todos los ejemplos siguientes son **DEDUCIDOS**, salvo las referencias explícitas a pruebas históricas. «Válido» significa válido en el modelo declarado, no ejecutado en el cliente durante esta investigación. «Óptimo» significa óptimo demostrado del objetivo principal dentro de ese modelo. No se certifica optimalidad secundaria salvo indicación expresa.

Para reproducirlos en el futuro, usar una carpeta limpia por caso, `Mode:0`, formato v14, samples del beatmap habilitados y `LayeredHitSounds:1`, sin mods. A, B, C, D y E son audios diferentes, no mezclas: pueden prepararse como WAV PCM mono de 48 kHz y 100 ms con tonos distintos y amplitud baja para no saturar. La forma exacta y sus hashes deben fijarse en el manifiesto experimental. `N/W/F/C` significa HitNormal/Whistle/Finish/Clap; `n/s/d` significa banco Normal/Soft/Drum. Un archivo universal mantiene el tipo, pero omite banco e índice.

Los conteos incluyen archivos de samples requeridos, no el audio principal ni `.osu`. Un custom puede nombrar una ruta estándar existente. No se admiten premixes, alias de sistema de archivos, archivos nuevos de silencio, sonidos extra ni cambios de objetos. Las opciones que cambian tipo quedan condicionadas a la caracterización de §8; nunca se infiere que toda permutación de canales es audible-equivalente en stable.

Cabecera mínima común para los fragmentos, salvo cambios expresos:

```ini
osu file format v14

[General]
AudioFilename: backing.wav
Mode: 0
SampleSet: Normal

[Metadata]
Title: Research fixture
Artist: Synthetic
Creator: Research
Version: Case

[Difficulty]
HPDrainRate:5
CircleSize:4
OverallDifficulty:5
ApproachRate:5
SliderMultiplier:1
SliderTickRate:1

[TimingPoints]
0,500,4,1,1,100,1,0

[HitObjects]
```

`backing.wav` debe cubrir la duración de la prueba. Los archivos fuente A/B/etc. no deben permanecer como copias adicionales en la carpeta exportada si no se referencian. Estos fragmentos son datos de prueba conceptual; no son código de producción ni fixtures incorporadas al repositorio.

### 4.1 Un sample repetido

**Deseado:** A una vez al clicar cada círculo, a 1000 y 1500 ms, volumen 70.

Archivos: `normal-hitnormal.wav=A`.

```text
256,192,1000,1,1,1:1:1:70:
256,192,1500,1,1,1:1:1:70:
```

Son dos voces potencialmente superpuestas si la duración de A excediera 500 ms. No son una voz persistente. Alternativa: ambos con filename `normal-hitnormal.wav`, sin añadir otro archivo; esta alternativa cambia de mecanismo y requiere verificar las condiciones de entorno.

**Archivos:** 1. **Óptimo principal demostrado:** A es local y debe existir al menos una vez; la construcción alcanza esa cota. **C:** 1 para la versión estándar. **Conflicto:** asignar un índice nuevo por ocurrencia multiplica copias sin necesidad. **Reutilización real:** todas las ocurrencias comparten recurso, conservando sus reproducciones.

### 4.2 Dos samples simultáneos

**Deseado:** A+B, una voz de cada uno, mismo círculo y volumen 70.

Archivos: `normal-hitnormal.wav=A`, `normal-hitwhistle.wav=B`.

```text
256,192,1000,1,3,1:1:1:70:
```

`3=1+2` hace explícito Normal. **Archivos:** 2. **Óptimo principal demostrado:** dos contenidos locales distintos requieren al menos dos archivos sin premix. **C:** 1.

**Inválido:** poner `A.wav` como filename y activar Whistle esperando A+B; D020 da solamente A. **Inválido:** querer A@70 y B@35 mediante los campos ordinarios del mismo objeto; hay un volumen común. La prueba `PlayHitSoundLayers_WhenTwoSamplesExist_MixesIndividualVolumes` muestra que el motor puede mezclar capas con volúmenes diferentes, no que el formato pueda expresar esa asignación en un círculo. [P12]

### 4.3 Tres y cuatro samples simultáneos

| Deseado por círculo | Máscara | Archivos | Número | Certificación en el modelo |
|---|---:|---|---:|---|
| A+B+C, todos a 70 | 7 | `normal-hitnormal.wav=A`, `normal-hitwhistle.wav=B`, `normal-hitfinish.wav=C` | 3 | Óptimo principal: tres contenidos distintos. |
| A+B+C+D, todos a 70 | 15 | Los tres anteriores y `normal-hitclap.wav=D` | 4 | Óptimo principal: cuatro contenidos distintos. |

Fragmentos: `256,192,1000,1,7,1:1:1:70:` o `256,192,1000,1,15,1:1:1:70:`. Ejecutar cada alternativa en carpeta separada para que el conteo sea exacto.

**Conflicto:** si B/C/D están en tres bancos distintos, la máscara no permite seleccionarlos independientemente. **Reutilización:** esos mismos archivos pueden servir otras máscaras que sean subconjuntos con A presente. **Contraejemplo:** «cuatro archivos bastan para cualquier colección de combinaciones» es falso; los tipos de canal son fijos en los nombres, los bancos de adiciones están acoplados y Normal no es opcional en este modelo.

### 4.4 Cinco o más sonidos en un instante

**Variante A, un círculo:** A+B+C+D+E como cinco ocurrencias independientes del mismo disparador. **Inviable en el modelo conservador**, sin importar cuántos índices se creen. El círculo selecciona un solo índice y cuatro tipos; custom-only selecciona un único archivo. Esta es una prueba de imposibilidad por capacidad, no una prueba del límite de canales del motor de audio.

**Variante B, dos objetos ya existentes:** O1 requiere A+B+C+D y O2 requiere E al mismo tiempo nominal. Archivos del caso de cuatro más `E.wav`. O1 usa máscara 15; O2 `0:0:0:70:E.wav`. **5 archivos**, óptimo principal por cinco contenidos distintos; la serialización representa cinco solicitudes. El sonido real depende de poder activar ambos objetos, y esa capacidad debe verificarse; no se recomienda introducir notas superpuestas para conseguirla.

**Variante C, slider:** una cabeza de slider puede generar cuatro golpes y el comienzo de un loop de cuerpo. Ya hay cinco componentes sonoros sin cinco adiciones en un mismo golpe. El loop no puede convertirse en una quinta percusión: duración y condición de seguimiento son distintas.

**Atajos rechazados:** añadir un objeto cambia jugabilidad; storyboard cambia el disparador; un premix E=A+B+C+D+E pierde la multiplicidad y las identidades exigidas, aunque una captura de autoplay pudiera parecerse. Una futura función explícita de renderizado compuesto sería otro problema, no una optimización transparente autorizada por D004.

### 4.5 Reutilización entre combinaciones: triángulo que obliga a duplicar

**Deseado:** en tres círculos separados, `{A,B}`, `{A,C}`, `{B,C}`; ambas capas a 70. No hay contenido externo, silencio añadido ni premix. Normal debe estar presente y transportar una de las dos identidades deseadas.

Construcción de cuatro archivos:

| Archivo | Contenido | Uso |
|---|---|---|
| `normal-hitnormal.wav` | A | Normal en AB y AC, índice 1. |
| `normal-hitnormal2.wav` | B | Normal en BC, índice 2. |
| `hitwhistle.wav` | B | Whistle universal en AB. |
| `hitclap.wav` | C | Clap universal en AC y BC. |

```text
256,192,1000,1,3,1:1:1:70:
256,192,2000,1,9,1:1:1:70:
256,192,3000,1,9,1:1:2:70:
```

No hay específicos de Clap ni Whistle que oculten los universales. **F=4, U=3, R=1, C=2.** Hay otra construcción de cuatro usando dos bancos y un índice; también tiene dos parejas. No se usa ese hecho como prueba general de optimalidad secundaria.

**Prueba del mínimo de archivos:** supóngase F=3. Cada identidad tendría exactamente un archivo. Para participar en una pareja ordinaria, ese archivo tiene un tipo fijo: Normal o adición. Cada pareja requiere exactamente un Normal y una adición. AB obliga a que A y B estén en lados opuestos; AC obliga a que A y C estén en lados opuestos, así que B y C quedan en el mismo lado. BC se vuelve imposible. Un universal no cambia de tipo; un filename explícito no produce una pareja. Por contradicción, F≥4. La construcción alcanza F=4.

**Resultado:** óptimo principal demostrado para el submodelo, todavía sin certificación de las condiciones audibles de todas las representaciones alternativas en stable. Si se conserva un contrato de fallback por canal que impida estas reasignaciones, habrá que optimizar dentro de ese dominio más estrecho.

Este caso descarta «una copia por sample siempre basta» y «basta analizar el máximo de simultaneidad». La incompatibilidad aparece entre eventos distintos. También descarta un grafo de cuatro colores sin la obligación Normal: ese grafo colorearía el triángulo con tres colores, pero seguiría sin resolver las tres parejas legales con tres archivos.

### 4.6 Bancos e índices: conflictos y universal útil

**A. Bancos de adiciones.** Supóngase A en `normal-hitnormal.wav`, B en `soft-hitwhistle.wav` y C en `drum-hitfinish.wav`. Un círculo A+B+C no puede elegir `additionSet=Soft` para W y `additionSet=Drum` para F. Debe reorganizar los archivos para que ambas adiciones compartan banco e índice, por ejemplo B en `soft-hitwhistle.wav` y C en `soft-hitfinish.wav`. Son **3 archivos** si el anterior C se puede retirar y todas sus referencias se actualizan correctamente. Si la ruta anterior está protegida por otro consumidor, esa construcción necesita **4**; no se afirma que sea óptima entre todas las demás opciones.

**B. Índices.** Eventos A+B en i=1 y A+C en i=2, con B y C obligados al mismo tipo Whistle. Un plan específico crea:

`normal-hitnormal.wav=A`, `normal-hitwhistle.wav=B`, `normal-hitnormal2.wav=A`, `normal-hitwhistle2.wav=C`.

Son **4 archivos**. Reemplazar las dos copias de A por `hitnormal.wav=A` deja **3 archivos** y mantiene los Whistle específicos. **Óptimo principal** para esos tres contenidos locales; C permanece en 2. Esta deduplicación mantiene el tipo Normal y no necesita permutar canales.

**Conflicto global:** si en otra dificultad un Normal de i=3 dependía del skin, el nuevo `hitnormal.wav` lo sustituye. Entonces esa transformación es inválida a menos que se preserve esa consulta por otro medio permitido. «Universal» significa fallback físico amplio, no alias privado de un grupo.

### 4.7 Herencia de timing points

Usar:

```text
[TimingPoints]
0,500,4,1,1,70,1,0
1800,-100,4,2,2,40,0,0

[HitObjects]
256,192,1000,1,9,0:0:0:0:
256,192,2000,1,9,0:0:0:0:
```

**Deseado:** primer círculo A+B a 70; segundo A+C a 40. Archivos: `hitnormal.wav=A`, `normal-hitclap.wav=B`, `soft-hitclap2.wav=C`. **3 archivos**, óptimo principal para A/B/C; C=2. El primer evento usa `(Normal,1)` y el segundo `(Soft,2)`.

**Válido en este caso:** mantener herencia o escribir los valores efectivos en cada círculo, siempre que no se cambie la política de consultas ni la evaluación temporal. **Inválido:** convertir todos los índices escritos 0 en 1, o «simplificar» el segundo timing point perdiendo índice, volumen o banco.

**Contraejemplo adicional:** un objeto con índice escrito 0 y timing efectivo 0 debe conservar búsqueda externa incluso si `hitnormal.wav` existe. El mismo objeto con índice escrito 1 no conserva esa conducta. Reescribir un círculo no autoriza la misma técnica en un borde de slider: D021 no le ofrece override de índice por borde.

### 4.8 Slider: bordes, ticks y cuerpo

En la cabecera común, usar estos puntos y este slider:

```text
[TimingPoints]
0,500,4,1,1,70,1,0
1800,-100,4,2,2,40,0,0

[HitObjects]
100,100,1000,2,2,L|300:100,2,200,8|4|2,0:0|0:0|0:0,1:1:99:20:ignored.wav
```

La duración de cada span es `200/(1×100×1)×500 = 1000 ms`. Hay dos spans. El punto a 1800 conserva SV=1. Resultado esperado por D021 y las fórmulas del proyecto:

| Componente | Tiempo o intervalo nominal | Banco/índice | Volumen | Ruta(s) específica(s) |
|---|---|---|---:|---|
| Cabeza N+C | 1000 | Normal,1 | 70 | `normal-hitnormal.wav`, `normal-hitclap.wav` |
| Repetición N+F | 2000 | Soft,2 | 40 | `soft-hitnormal2.wav`, `soft-hitfinish2.wav` |
| Cola N+W | 3000 | Soft,2 | 40 | `soft-hitnormal2.wav`, `soft-hitwhistle2.wav` |
| Ticks | 1500 y 2500 | Normal,1 | 70 | `normal-slidertick.wav` |
| Cuerpo Slide+Whistle | [1000,3000), sujeto a actividad | Normal,1 | 70 | `normal-sliderslide.wav`, `normal-sliderwhistle.wav` |

Si esas ocho rutas contienen **ocho contenidos distintos**, F=8 y U=8: óptimo principal por identidad. Se reutiliza Soft-Normal entre repetición y cola, y el tick entre spans. Los dos loops generan múltiples periodos sin múltiples archivos. `99`, `20` y `ignored.wav` no se aplican; no hace falta un archivo `ignored.wav` para este slider.

**Conflictos:** poner un punto temporal en la cola puede alterar otros bordes/círculos heredados; cambiar el flag Whistle del slider puede modificar el cuerpo; mover un sample de tick a cabeza cambia tiempo y disparador. Convertir todo a una pista plana elimina el seguimiento del jugador. Cambiar un loop por otra codificación de «igual sonido» puede alterar duración del periodo, priming o fase.

**Ámbito de optimalidad:** ocho contenidos diferentes en las ocho funciones, sin transformar audio; no se afirma que cualquier slider requiera ocho archivos. Con identidades repetidas entre funciones puede haber duplicación forzosa por los distintos nombres/tipos, y los universales solo comparten dentro del mismo tipo.

### 4.9 Varias dificultades y recursos compartidos

**Caso seguro:** Easy y Hard necesitan A como Normal; Easy necesita B como Clap y Hard necesita C como Whistle, siempre i=1. Ambas pueden compartir `normal-hitnormal.wav=A`; junto con `normal-hitclap.wav=B` y `normal-hitwhistle.wav=C` hay **3 archivos en el paquete**, no 2+2=4. Óptimo principal por tres contenidos locales distintos.

**Colisión:** si cada dificultad exporta por separado `normal-hitclap.wav` con distinto audio, unir carpetas sobrescribe una intención. Debe asignarse otra posición legal o resolverse globalmente.

**Sombreado:** Easy usa `soft-hitclap2.wav=B`; Hard consulta Soft-Clap i=3 y necesita fallback externo. Exportar `hitclap.wav=B` para reducir copias puede cambiar Hard aunque su `.osu` no se haya tocado. Conservar el específico de i=2 cuesta un archivo y preserva Hard; el universal no es una mejora válida en ese contexto.

**Referencias externas al subsistema:** una ruta también puede aparecer en `.osb`, `[Events]`, un filename explícito u otro archivo compartido. No borrar el recurso por no aparecer en las capas de una sola dificultad. Optimizar una dificultad dentro de una carpeta incompletamente inventariada requiere inmovilizar los recursos compartidos o producir un paquete separado; no se puede prometer el óptimo global del beatmapset.

### 4.10 Multiplicidad del mismo contenido

**A. Mismo disparador, A+A:** `normal-hitnormal.wav=A` y `normal-hitwhistle.wav=A`; círculo con máscara 3, volumen 40. **2 rutas, 1 identidad y 2 ocurrencias.** En el submodelo ordinario son necesarias dos posiciones de tipo distinto: un bit no puede activarse dos veces y un universal no cubre ambos tipos. F=2 es óptimo en ese submodelo.

Reemplazarlo por una sola A@80 viola multiplicidad incluso en un mezclador lineal donde pudiera igualar una suma particular. A@60+A@60 tampoco cabe como A@120 en el rango habitual. Clipping, límites de volumen y comportamiento de voces aportan más razones para no identificar esas operaciones.

**B. Dos objetos ya existentes, A en ambos:** los dos pueden apuntar por filename al mismo `A.wav`. **1 ruta, 2 ocurrencias**; óptimo de recursos. Si uno se activa y otro no, debe sonar una sola ocurrencia. Si se activan ambos, el modelo solicita dos.

**DESCONOCIDO, requiere prueba stable:** al disparar simultáneamente el mismo recurso, cuánto suman realmente las voces y si hay supresión, reuso de canales o límites. El motor propio abre lectores por reproducción; esto no certifica la política de concurrencia de stable. Probar también dos nombres con bytes idénticos frente a un único nombre compartido: si el cliente trata distinto el recurso compartido, la deduplicación puede cambiar el audio aunque el multiconjunto abstracto coincida.

### 4.11 Spinners y datos no cubiertos por la enumeración actual

**CONFIRMADO histórico:** la fixture del proyecto contiene spinner de 2000 a 4000 ms, máscara 14 y filename explícito; registra Custom-only al final en stable y Custom+adiciones en lazer. [P8b]

**DEDUCIDO:** para el sonido final custom se necesita **1 archivo** local; es el mínimo para ese componente. Esto no cuenta `spinnerspin` ni `spinnerbonus`, que tienen activación y modificación de frecuencia propias. No se propone tratarlos como ranuras ordinarias extra.

**CONFIRMADO en código:** `GetTimedHitSoundEventsInRange` solo enumera círculos, bordes y ticks. **DEDUCIDO:** comparar únicamente esa lista puede declarar equivalentes dos mapas con distintos sonidos finales de spinner. Además, el método genérico de índice/volumen consulta `hitObject.Time`, mientras el decoder legacy oficial consulta el final para no repetidores. Un punto de sample a mitad del spinner es una fixture prioritaria, no una corrección aplicada en esta investigación. [P3][O11]

### 4.12 Resumen de cotas y resultados

| Caso | F de construcción | ¿Óptimo principal? | Límite o condición principal |
|---|---:|---|---|
| A repetido | 1 | Sí, en el modelo | A local sin premix. |
| A+B | 2 | Sí | Mismo volumen y disparador; N+adición. |
| A+B+C / A+B+C+D | 3 / 4 | Sí | Bancos/índice compatibles. |
| Cinco identidades en un círculo | No existe | Inviabilidad demostrada | Sin objetos extra, premix ni storyboard. |
| Cinco en dos objetos existentes | 5 | Sí, recursos | Conservar activación de cada objeto; validar concurrencia. |
| AB, AC, BC | 4 | Sí | Prueba del triángulo; entorno y transformaciones declarados. |
| A compartido entre i=1 e i=2 | 3 | Sí | Universal no afecta consultas protegidas. |
| Herencia de dos secciones | 3 | Sí | Tres identidades locales. |
| Slider completo de §4.8 | 8 | Sí | Ocho contenidos distintos. |
| Dos dificultades de §4.9 | 3 | Sí | Carpeta completa e inventariada. |
| A+A en un círculo | 2 | Sí, submodelo ordinario | Dos ocurrencias; no basta hash único. |
| A en dos objetos por mismo filename | 1 | Sí, recursos | Resultado de voces stable pendiente de medición. |

## 5. Comparación de estrategias

### 5.1 Coste y garantías

Definir `n` como ocurrencias expandidas, `g` como grupos con restricciones diferentes, `k_g` como alternativas por grupo, `p` como rutas candidatas y `d` como dificultades. Combinaciones idénticas solo se pueden agrupar si también coinciden restricciones de tipo, volumen, herencia y comportamiento externo.

| Estrategia | Garantía posible | Coste orientativo | Determinismo y mantenimiento | Valor para el proyecto |
|---|---|---|---|---|
| Preservar representación original | Validez para entradas soportadas y recursos copiados correctamente; ningún óptimo. | Parse/resolución e inventario; aproximadamente proporcional a eventos, referencias y bytes. | Muy alto, pocas decisiones. | Baseline imprescindible del exportador. |
| Asignación determinista simple | Válida si cada patrón y sus efectos globales se verifican. | Escanear alternativas y consultas afectadas; no prometer O(n) si se recalcula todo. | Alta; orden y desempates explícitos. | Primer candidato, pero evitar un índice nuevo por evento como política permanente. |
| Heurística de reutilización | Solo mejora válida; no garantía global. | Una pasada considera `Σk_g` alternativas y su costo marginal. Verificar puede dominar el tiempo. | Alta si no usa orden de filesystem/hash aleatorio. | Priorizar ahorro de archivos; C solo desempata. |
| Voraz con reconsideración | Puede superar mínimos locales; sin óptimo general. | Reasignar h grupos implica hasta `k^h` alternativas en un vecindario uniforme. | Medio; mantener h y presupuesto fijos. | Útil cuando una copia requiere mover dos asignaciones a la vez. |
| Búsqueda exhaustiva / branch-and-bound | Óptimo si se explora/poda correctamente todo el dominio. | Peor caso `∏g k_g`; memoria DFS moderada, memoria de memoización potencialmente grande. | Alta con orden estable; prueba relativamente explicable. | Oráculo futuro para instancias pequeñas y contraejemplos. |
| Programación entera | Óptimo o solución/cota según terminación; modelo de ausencia y precedencia obligatorio. | Variables del orden `Σk_g + p·|S| + |Pairs|`; resolver puede ser exponencial. | Modelo revisable, dependencia adicional. | Alternativa si el patrón binario resulta natural y eficiente. |
| CP-SAT | Misma distinción entre válido y óptimo; no resuelve semántica desconocida. | Depende de booleanos, simetrías y propagación; requiere benchmarks. | Fijar versión/configuración y desempate; paralelismo y corte por tiempo pueden variar resultados. | Justificable más adelante, no antes de D005. |
| Híbrida | Heurística válida más exactitud en componentes o instancias pequeñas. | Presupuesto controlable; cotas parciales combinables cuando componentes son realmente independientes. | Medio; separar candidato, verificador y certificación. | Recomendación posterior al round-trip. |

**CONFIRMADO documentalmente:** OR-Tools distingue `OPTIMAL`, `FEASIBLE`, `INFEASIBLE`, `MODEL_INVALID` y `UNKNOWN`; CP-SAT trabaja con enteros. [A1] **DEDUCIDO:** aquí no hace falta cuantizar tiempos fraccionarios al solver si los patrones ya contienen tiempos y consultas resueltos: el solver puede elegir patrones con variables binarias. Convertir tiempos a enteros sin un contrato de precisión sería un error semántico.

### 5.2 Por qué una heurística puede fallar

El costo marginal de una alternativa depende de qué archivos ya existen. Pero una ausencia también tiene valor: introducir un universal puede invalidar un fallback externo. Por tanto, la función no es una simple ganancia de cobertura creciente.

Un voraz que empieza colocando B como adición en AB y AC puede necesitar después una copia de B para BC, como en el triángulo. Ese ejemplo demuestra una copia necesaria; no demuestra por sí solo que el voraz sea subóptimo. Para demostrar suboptimalidad de una implementación concreta habrá que comparar su resultado con enumeración exacta sobre sus propias reglas y orden de entrada. No se inventa aquí un benchmark ni un factor de aproximación.

Sí se demuestra un fallo de la política «un índice específico por combinación»: en §4.6B genera cuatro archivos donde tres bastan. Y la política «convertir toda muestra frecuente en universal» puede producir una solución inválida por §4.9.

### 5.3 Escalabilidad y simetrías

**DEDUCIDO:** repetir 10 000 veces una combinación no equivale a tener 10 000 decisiones independientes. Tras construir un inventario que mantenga cada ocurrencia para verificarla, pueden agruparse restricciones equivalentes para buscar. En cambio, muchas combinaciones únicas, máscaras diferentes, bancos fijados, herencia compartida y dificultades incrementan el acoplamiento.

Medidas razonables para evaluar en el futuro:

- Agrupar patrones equivalentes sin perder multiplicidad ni contexto temporal.
- Eliminar candidatos dominados solo con una prueba de dominancia que incluya fallbacks y efectos sobre otras dificultades.
- Usar la representación original como incumbente válida y cota superior de F.
- Usar U como cota inferior básica para contenidos locales; añadir cotas demostradas de incompatibilidad, como el triángulo, sin sumarlas si comparten recursos.
- Renumerar índices simétricos con una regla canónica solo si no hay referencias o posiciones protegidas que distingan esos índices.
- Separar componentes únicamente cuando no compartan rutas candidatas, universales, referencias protegidas ni decisiones de timing. Separar por dificultad o por sample a priori no es correcto.

Un rango artificial de índices `1..K` limita la prueba de optimalidad. Si se quiere llamar global al resultado, hay que justificar que K representa todas las soluciones relevantes, por ejemplo mediante una cota y renombrado de índices realmente intercambiables. No se propone un K universal sin esa prueba.

### 5.4 Exactitud pequeña y heurística grande

**DEDUCIDO:** sí es viable como estrategia, pero el umbral debe depender de `∏k_g`, el número de rutas y el acoplamiento, no de un número de samples elegido sin mediciones. Una instancia de pocos samples y muchas restricciones puede ser más difícil que otra grande con repeticiones triviales.

Para una instancia pequeña, agotar la búsqueda o alcanzar una cota inferior igual al costo de la construcción prueba el óptimo. Para una grande, devolver una solución verificada, su costo `(R,C)`, cota disponible y razón de parada. Si el solver no certifica optimalidad, no mostrar «óptimo». Si no encuentra nada antes del límite, conservar la solución original válida; «no encontrado» no significa «inviable».

## 6. Recomendación técnica fundamentada

**Mantener D005. No introducir ahora CP-SAT, un optimizador global ni nuevas capas de proyectos para alojarlo.** El beneficio inmediato está en identificar qué debe sobrevivir a la edición y a la exportación, y en caracterizar las discrepancias de resolución que un round-trip interno podría ocultar.

Secuencia recomendada para una futura etapa autorizada:

1. **Contrato y dominio admitido.** Definir si se promete preservación bajo samples locales habilitados o también bajo distintas skins/opciones; conservar disparadores, voces y datos no editados. Las entradas fuera del dominio se preservan opacamente o se rechazan de forma explícita, sin exportación silenciosamente incompleta.
2. **Exportación conservadora.** Producir primero la misma representación y recursos necesarios; mantener texto/datos no interpretados. Verificar reload con archivos realmente exportados.
3. **Identidad y equivalencia.** Resolver contenido local de manera reproducible y mantener referencias externas simbólicas. Separar cambios físicos permitidos de propiedades lógicas observables. Completar cuerpos, spinners y referencias compartidas antes de afirmar equivalencia del mapa completo.
4. **Caracterización stable.** Resolver los puntos de §8 que bloquean las transformaciones candidatas. El resultado podría reducir el espacio de búsqueda; eso es una mejora de corrección, no un fracaso de optimización.
5. **Optimización limitada.** Probar reutilización de rutas custom ya compatibles, fusión de ubicaciones realmente equivalentes y universales únicamente con inventario global. Cada cambio debe reducir `(R,C)` o cumplir un desempate explícito y superar el verificador.
6. **Oráculo pequeño.** Añadir, cuando D005 esté cumplida, enumeración conceptual convertida por el autor a una herramienta/prueba acotada. Usarla para encontrar contraejemplos a la heurística y medir la brecha.
7. **Escalar solo con evidencia.** Adoptar búsqueda más amplia o CP-SAT si el corpus muestra ahorros materiales que la heurística pierde, el tiempo de búsqueda propio no es aceptable y las restricciones ya tienen semántica estable.

Pseudocódigo conceptual, no implementación:

```text
leer paquete e inventario completo
construir intención y referencias protegidas
crear una exportación conservadora válida
verificar reload y equivalencia

cuando D005 esté cumplida:
    enumerar transformaciones permitidas en orden estable
    para cada candidata:
        resolver nuevamente sus consumidores y efectos globales
        rechazar si cambia cualquier invariante
        aceptar solo mejora lexicográfica autorizada
    verificar la salida completa desde sus archivos
    informar costo, cotas y alcance de la garantía
```

La comparación antes/después debe incluir número de rutas exportadas, contenido único, redundancia, parejas efectivas, bytes y referencias externas. La vista previa de audio nunca debe sustituir esa comprobación.

## 7. Impacto arquitectónico

### 7.1 Separaciones existentes que conviene conservar

**CONFIRMADO:** `BeatmapLoader` interpreta el texto; `Beatmap` resuelve tiempo y capas; `SampleResolver` se ocupa de rutas y fallback; `HitSoundPlaybackService` coordina reproducción; `AudioPlaybackEngine` administra audio/dispositivo/lectores. `TimedHitSoundEvent` preserva el objeto originario y el índice de borde, y los cuerpos se tratan separadamente. Son límites concretos con consumidores actuales, coherentes con D016, D018 y D026. [P3]–[P7][P11][P13][P14]

**Recomendación:** conservarlos. La identidad y la intención exportable no deben obtenerse de buffers NAudio, del cursor de reproducción ni de una captura ya mezclada. La comparación lógica debe poder ejecutarse sin abrir WASAPI. Tampoco hace falta dividir el proyecto en varios ensamblados para mantener esa independencia conceptual.

### 7.2 Carencias verificables y su beneficio al resolverlas

| Hallazgo en la revisión publicada | Estado | Por qué importa | Requisito futuro justificado |
|---|---|---|---|
| `HitSoundLayer` solo contiene Type, SampleSet, SampleIndex, Volume y Filename. | **CONFIRMADO** [P6] | Dos rutas con A no se reconocen como la misma identidad; renumerar puede parecer cambio lógico. | Relacionar la intención con identidad estable y equivalencia explícita cuando edición/exportación sean consumidores. No usar esos campos físicos como SampleId. |
| El resultado físico contiene Outcome, ResolvedPath y FallbackFilename. | **CONFIRMADO** [P13] | Una ruta resuelta no dice por sí sola qué contenido o versión tenía, ni todos los candidatos que podrían interceptarla. | Conservar inventario/fingerprint y trazabilidad de búsqueda en la verificación; no sobrecargar identidad con ruta absoluta. |
| `BeatmapLoader` no conserva curva de slider (`parts[5]`), meter/effects del timing, `[Events]`, `[Colours]` ni todos los campos de General/Metadata. | **CONFIRMADO por lectura** [P5] | Reconstruir el archivo solo desde el modelo actual puede cambiar jugabilidad, storyboard, kiai o metadatos. | Elegir preservación de documento original con cambios localizados o ampliar el modelo serializable. Beneficio: round-trip sin pérdida. |
| Los sliders se parsean solo cuando `parts.Length > 10`. | **CONFIRMADO** [P5], 238 | Formas con campos opcionales omitidos quedan sin datos suficientes aunque el formato pueda admitirlas. | Definir dominio admitido, defaults y tratamiento de entradas parciales antes de anunciar exportación general. |
| `GetTimedHitSoundEventsInRange` no incorpora spinner-end; los loops se obtienen aparte. | **CONFIRMADO** [P3], 640–708 | Esa lista sola no es la intención completa del beatmap. | El contrato de equivalencia debe combinar eventos discretos, cuerpos continuos y componentes preservados fuera de cobertura. |
| Objetos y listas son mutables, y los eventos mantienen referencias a `HitObject`. | **CONFIRMADO** [P3][P6b] | Editar durante una cola o comparación puede mezclar revisiones. | Definir propiedad del estado, snapshots/revisión y momento de invalidación cuando exista edición WPF. No es obligatorio crear una jerarquía de servicios. |
| El motor admite volumen individual por capa y loops finitos. | **CONFIRMADO** [P11][P12] | La vista previa puede reproducir una intención no expresable en un solo objeto legacy. | Distinguir «reproducible por el editor» de «exportable a osu!». Mostrar conflictos de representación sin corregirlos mediante mezclas silenciosas. |
| Los métodos genéricos de herencia consultan `GetActiveTimingPoint`; los de bordes tienen helper con fallback al primer punto. | **CONFIRMADO** [P3], 35–74 y 327–409 | Puede haber diferencias en pre-primer punto, defaults y spinners. | Caracterizar y documentar la semántica por tipo antes de confiar en una equivalencia global. |

El término «logical layer» de la documentación describe hoy una capa **resuelta semánticamente** pero aún identificada mediante datos del formato. No constituye todavía la representación independiente de recursos necesaria para optimizar. Eso no invalida la separación actual; identifica el siguiente contrato que necesitarán sus futuros consumidores.

### 7.3 Discrepancias que una prueba de round-trip interna podría ocultar

1. **Normal implícito.** `GetHitSoundTypes` agrega Normal siempre y `HitSoundLayer` no conserva `IsLayered`. El `hitSound` original sí permanece en el objeto. **DEDUCIDO:** perder la distinción al exportar puede modificar `LayeredHitSounds=0`. No inferir que la decisión D020 resuelve esta cuestión: trata otro mecanismo. [P3][O2][O6][O7]
2. **Antes del primer timing point.** El helper de slider elige el primer punto para índice/volumen, pero cuerpo/tick obtienen banco mediante los métodos genéricos. Con Default=Normal, primer punto Soft, slider anterior y trailing NormalSet=0, el código deriva banco Normal y el índice/volumen del primer punto. Las pruebas pre-primer punto leídas fijan NormalSet=1, por lo que no prueban esa combinación. **DEDUCIDO del código; conformidad stable pendiente.** [P3][P10]
3. **Círculo sin punto temporal.** Si volumen escrito=0 y no hay punto activo, el helper genérico retorna 0; el motor lo aplica como cero. Los bordes/cuerpo/ticks usan fallback 100 en ausencia de timing. **CONFIRMADO estructuralmente; significado legacy completo por verificar.** No inventar que todos los objetos usan ya el mismo default. [P3][P11]
4. **Ventana temporal y spinner.** El decoder oficial legacy de lazer usa tolerancia de 5 ms y, para no repetidores, tiempo final. El proyecto usa inicio exacto en el acceso genérico. **CONFIRMADA la diferencia de implementación; DESCONOCIDO su alcance exacto en stable actual sin nueva fixture.** [O11][P3]
5. **Volumen audible.** La normalización lineal propia no modela el mínimo documentado en los clientes. Un round-trip que preserve un valor puede ser correcto como exportación, aunque la preview todavía no reproduzca el volumen del cliente. Son garantías diferentes. [P11][O12]
6. **Custom ausente.** El producto lo informa y no intenta reproducción; la prueba oficial de lazer `TestFileSampleFallsBackToNormal` espera fallback a Normal. **CONFIRMADO en ambos proyectos, pero no una orden de cambiar D017.** El estado físico del editor y la reacción final del cliente deben mantenerse diferenciados. [P4][P7][O8]

Estas observaciones justifican pruebas focalizadas; no implican que se hayan ejecutado, que todas sean bugs de stable o que esté autorizada su reparación.

### 7.4 Qué decidir antes de WPF y qué posponer

En el hito ya previsto por D027, conviene fijar:

- **Identidad y edición:** qué entidad selecciona el usuario, cómo conserva identidad al renombrar/reubicar y qué significa reemplazar audio. Beneficio: undo/redo y referencias no dependerán de nombres exportados.
- **Conservación del documento:** cómo sobreviven las partes no editadas. Beneficio: la UI no queda atada a un modelo que no puede volver a escribir el beatmap completo.
- **Unidad de operación:** dificultad o beatmapset; qué recursos están compartidos/protegidos. Beneficio: editar una dificultad no rompe otra al guardar.
- **Intención versus factibilidad:** permitir detectar que una combinación/volumen no cabe en legacy sin inventar objetos o premixes. Beneficio: advertencias concretas antes de exportar.
- **Revisión del estado y reproducción:** quién autoriza leer una revisión mientras se edita otra. Beneficio: comparaciones reproducibles y audio coherente con la vista seleccionada.

**Posponer:** algoritmos globales, CP-SAT, una interfaz por cada resolver, un modo de compatibilidad stable/lazer, transcodificación, deduplicación perceptual, cachés complejas y proyectos nuevos para capas vacías. No hay evidencia que justifique separar `Beatmap.cs` únicamente por tamaño. Tampoco este informe reemplaza el próximo trabajo registrado de Audio Infrastructure ni inicia la revisión arquitectónica formal.

## 8. Equivalencia y plan de verificación futura

### 8.1 Tres equivalencias diferentes

| Equivalencia | Definición verificable | Lo que no demuestra |
|---|---|---|
| Lógica | Correspondencia de ocurrencias/intervalos por identidad, multiplicidad, tiempo/regla temporal, disparador, volumen y parámetros; misma conducta de referencias externas en el entorno contratado. | No prueba que el parser/resolver modele correctamente stable. |
| De archivos | Igualdad de bytes por archivo o igualdad de contenido según criterio explícito. Para exportación optimizada se exige preservación de contenidos requeridos y referencias, no la misma lista de rutas. | Dos carpetas con mismos hashes únicos pueden tener nombres incompatibles o distinta multiplicidad de voces. |
| Audible | Igual respuesta sonora bajo entradas de jugador y configuraciones especificadas; en una prueba controlada, comparación temporal y de señales según criterio definido. | Parecido perceptual o una captura de autoplay no prueba igualdad lógica ni comportamiento en otras configuraciones. |

Definir `Resolve(B,F,ω,u)` como la traza de reproducciones resuelta para beatmap B, archivos F, entorno ω y entrada de jugador u. La afirmación fuerte es:

`∀ω∈Ω, ∀u∈Inputs: Resolve(original,filesOriginal,ω,u) ≡ Resolve(exported,filesExported,ω,u)`.

`Inputs` es el conjunto de entradas del jugador contempladas; no es el número U de clases de audio de §3.5.

Una comprobación de editor con reproducción perfecta verifica una restricción de ese contrato. Para hacerlo tratable, conservar estructuralmente disparadores, geometría y objetos, y comparar condiciones simbólicas en vez de enumerar toda entrada humana. Por ejemplo, conservar que una capa se dispara por la pulsación de O17, no por «algún evento a 2000 ms».

No exigir igualdad del enum físico Type si se permite reubicar canales y ya se ha demostrado equivalencia en el entorno contratado; sí conservar todas sus consecuencias observables. Para políticas de fallback por canal, el tipo/banco puede volver a ser una parte necesaria de la intención simbólica. Esta distinción evita tanto aceptar cambios inválidos como rechazar cualquier optimización por el mero cambio de nombre.

### 8.2 Verificación determinista del pipeline

Procedimiento futuro:

1. **Capturar entrada:** archivo fuente, versión de formato, todas las dificultades incluidas, recursos, hashes y opciones de compatibilidad. Registrar entradas no soportadas, ausentes o corruptas.
2. **Parsear sin perder lo no editado:** mantener datos necesarios para reconstruir el beatmap, incluido orden significativo. Resolver herencia con una política versionada y definida.
3. **Construir intención normalizada:** eventos como multiconjunto e intervalos continuos; resolver cada contenido local y conservar referencias externas. Registrar origen objeto/borde/tick y procedencia de valores heredados.
4. **Exportar al destino aislado:** serializar `.osu` y únicamente los recursos permitidos. No usar el inventario de la carpeta original para completar accidentalmente un recurso ausente del destino.
5. **Recargar desde la salida real:** ejecutar parsing y resolución sobre los archivos exportados, no sobre objetos en memoria del exportador.
6. **Comparar:** hacer correspondencia de objetos/componentes, multiplicidad, SampleId/clase de contenido, volumen, disparador, tiempos, duración/fase de loops, resultados externos y propiedades no sonoras protegidas.
7. **Verificar recursos globalmente:** cada lookup del paquete, precedencia entre nombres/extensiones, rutas custom y storyboard; ausencia de referencias rotas y archivos eliminados todavía usados.
8. **Informar:** primera divergencia explicable y lista completa cuando resulte útil; costos `(R,C,B)`, dominio y límite de la garantía. «No verificable» no es «equivalente».

Ordenar para comparar no significa eliminar duplicados. Para tiempos derivados, usar la misma precisión contractual y conservar fuentes exactas cuando sea posible. No introducir una tolerancia arbitraria de 1 ms que pueda esconder cambios de herencia. Cuando se valide una tolerancia numérica, comprobar también que no cruza discontinuidades del resolver.

El pipeline puede usar el mismo parser por coherencia, pero necesita un segundo nivel de **conformidad externa**. Dos ejecuciones del mismo error pueden coincidir perfectamente. Las fixtures de clientes y los ejemplos de frontera deben evitar ese punto ciego.

### 8.3 Plan de pruebas y evidencia requerida

**No ejecutado en esta investigación.** Cada experimento debe guardar versión/build exacta de stable o lazer, configuración de skin, `.osu/.osz`, manifiesto SHA-256 de audio, procedimiento, captura y resultado por caso. Las fixtures históricas conservadas sirven de base, pero sus documentos no contienen todos esos parámetros; completarlos al repetir no convierte el resultado antiguo en uno nuevo.

| Prueba futura | Procedimiento | Evidencia / criterio de aceptación |
|---|---|---|
| T01. N/W/F/C y bit Normal | Comparar máscaras 0, 1, 2, 3, 14 y 15 con `LayeredHitSounds` 0/1; usar audios distinguibles. | Matriz de capas realmente presentes. Separar objeto con Normal explícito de Normal implícito. |
| T02. Reasignación de canal | Copiar exactamente el mismo audio a N/W/F/C; mantener objeto, volumen y entorno; activar de uno en uno con controles que no añadan sonidos extra. | Capturas alineadas y medición de amplitud/duración; no aprobar todas las permutaciones a partir de una sola pareja. |
| T03. Precedencia completa | Cruzar índices efectivos 0/1/2, archivo específico/universal/skin/default, extensiones competidoras y custom exacto/ausente. | Recurso ganador identificado por firma audible y manifiesto; incluye específico OGG contra universal WAV. |
| T04. Herencia temporal | Ubicar controles a t−6, t−5, t−1, t, t+1, t+5, t+6 respecto al evento; repetir con borde fraccionario, puntos coincidentes, antes del primero y sin puntos. | Determinar política real stable; registrar orden de líneas y valores de todos los campos, sin redondear la evidencia. |
| T05. Slider completo | Usar §4.8; cambiar índice/volumen/banco dentro del slider, trailing fields y edgeSets; interrumpir seguimiento y reiniciar desde el interior. | Bordes, ticks, cuerpos, fase y duración comprobados por separado. Comparar preview de edición con gameplay. |
| T06. Volumen | Probar 0 heredado, 1, 5, 8, 50 y 100, con control temporal explícito y sin él. | Ganancia observada; distinguir semántica de 0, mínimo audible y sliders de volumen global. |
| T07. Multiplicidad | A+A por dos tipos, dos objetos con el mismo archivo, dos rutas con bytes idénticos; disparos simultáneos y separados por 1/5/10 ms. | Número y amplitud de voces, reentrada y posible supresión. Comparación entre identidad de recurso y de contenido. |
| T08. Fallback y skin | Repetir transformaciones con samples del beatmap habilitados/deshabilitados y dos skins con audios claramente diferentes. | El resultado coincide en todo Ω o se restringe la garantía. Nunca sustituir fallback por silencio en el verificador. |
| T09. Dificultades compartidas | Exportar juntas Easy/Hard, incluyendo una consulta que hoy cae a skin y otra que usa universal; añadir consumidor `.osb`. | Ningún consumidor cambia al añadir/eliminar/renombrar archivos. |
| T10. Spinner | Cambiar timing de samples entre inicio y fin; probar custom y flags, además de spin/bonus. | Tiempo de herencia y sonido final; separar sonidos continuos/modulados y diferencias lazer. |
| T11. Round-trip conservador | Recargar salida con inputs de curvas, Events, Colours, metadatos, kiai, campos opcionales y versiones soportadas. | Datos sonoros y no sonoros protegidos permanecen. Una entrada no soportada no se exporta como éxito íntegro. |
| T12. Exactitud del optimizador futuro | Enumerar instancias pequeñas, incluidas §4.5 y §4.6; comparar heurística contra óptimo y cotas. | Soluciones válidas, costo real por rutas y estados de optimalidad correctos. |
| T13. Repetibilidad | Repetir misma entrada, variar orden del filesystem y orden de procesamiento de dificultades. | Misma solución canónica o misma identidad de salida según contrato; no depender de un orden incidental. |
| T14. Recursos inválidos | Archivo existente vacío, corrupto, con extensión distinta del contenido o colisión de mayúsculas/rutas. | Distinguir «encontrado por nombre» de «decodificable»; no declarar equivalencia audible de un recurso que no se puede leer. |

Las pruebas de `SampleResolverTests` usan archivos de cero bytes para comprobar **selección de ruta**. Esto es correcto para ese alcance de prueba; no demuestra reproducción, y esos archivos no deben convertirse en ejemplos de samples válidos para exportación. [P9]

### 8.4 Corpus y medición de escalabilidad

Diseñar un corpus futuro con casos independientes de tamaño creciente —por ejemplo 10, 50, 200 y 1000 identidades— variando además cantidad de combinaciones únicas, repetición, universales, volumen, porcentaje de sliders y dificultades. Son tamaños de ensayo propuestos, no límites de capacidad ni estimaciones de rendimiento.

Medir tiempo de inventario, generación de patrones, búsqueda y verificación por separado; memoria; F/R/C/B; cota inferior/superior; transformaciones rechazadas y causa. Incluir casos con muchas repeticiones y pocos grupos, y otros con pocas identidades pero muchas restricciones. Comparar heurística contra exacto donde sea posible. No justificar una dependencia por tiempo total de audio si el cuello de botella real es enumerar combinaciones.

## 9. Preguntas abiertas y condiciones de aceptación

| Pregunta | Estado actual | Cómo resolverla / decisión que bloquea |
|---|---|---|
| ¿Qué skins/opciones debe cubrir exactamente «sin alterar comportamiento»? | **DESCONOCIDO como contrato completo del producto.** | Decidir Ω. Mientras tanto, preservar referencias externas y tipos; no afirmar equivalencia audible universal. |
| ¿Son intercambiables N/W/F/C con bytes idénticos en stable para todas las configuraciones relevantes? | **No demostrado; existen contraejemplos bajo cambios de skin/LayeredHitSounds.** | T01/T02/T08 antes de permitir esas permutaciones. |
| ¿El mismo recurso disparado a la vez conserva todas las voces tras deduplicar? | **DESCONOCIDO en stable.** | T07; podría introducir una restricción adicional por recurso, no por contenido. |
| ¿Qué hace stable con custom ausente/corrupto y colisiones de extensión? | **Modelo del proyecto conocido; respuesta final del cliente incompletamente establecida aquí.** | T03/T14. No fusionar estados D017. |
| ¿Qué tolerancia y tiempo de herencia usa stable en cada componente? | **Diferencia con decoder oficial de lazer identificada.** | T04/T10. No cambiar D021 basándose solo en lazer. |
| ¿Qué ocurre antes del primer timing point y con volumen heredado sin punto? | **Asimetrías del código verificadas; conformidad pendiente.** | Fixtures específicas para bancos y defaults, no solo índice/volumen explícitos. |
| ¿Se permiten muestras nuevas compuestas, silencio auxiliar o normalización? | **No autorizadas por esta investigación.** | Mantenerlas fuera. Si se piden después, requieren otro contrato de identidad/multiplicidad y otro objetivo. |
| ¿Cómo contar C para custom, índice 0, universales y beatmapsets? | **D004 define la prioridad, no todos los detalles métricos.** | Ratificar convención propuesta de §3.5 antes de certificar óptimos secundarios. |
| ¿Puede cambiarse el tipo sin modificar conducta en conversiones a otros modos? | **Fuera del objetivo canónico standard; no preservado automáticamente.** | Conservar flags si se requiere ese alcance. La validación de rankabilidad sigue siendo separada. |
| ¿Qué cota de índices/patrones hace completa una búsqueda global? | **No demostrada para el problema completo.** | Probarla para el dominio permitido o informar óptimo restringido. |
| ¿Cuál es el beneficio real de CP-SAT frente a un exacto pequeño/heurística? | **HIPÓTESIS; no hay benchmark de este problema.** | Corpus de §8.4 después de D005. |

### Conclusión técnica

La optimización puede formalizarse de forma rigurosa y puede ahorrar archivos, especialmente cuando un mismo contenido ocupa varias ubicaciones del mismo tipo o varias dificultades reutilizan recursos. También hay duplicación necesaria por la estructura del formato. El cuello de botella conceptual actual es definir y verificar la equivalencia completa, no escoger un solver.

La recomendación respeta D002, D003, D004, D005, D017, D020, D021 y D022: mantener separados contenido, representación y resolución; preservar multiplicidad y referencias externas; comprobar el paquete real; y no iniciar el optimizador global hasta que el ciclo exportar/recargar/verificar sea confiable. Las cuestiones de arquitectura identificadas son requisitos de información y conservación, no una autorización de refactorización.


## 10. Fuentes consultadas y referencias precisas

Los enlaces de código están fijados a SHA. Las referencias a tests describen su contenido o un registro histórico, no una ejecución nueva. Las fuentes oficiales de lazer se utilizan con ese alcance declarado. Las pruebas matemáticas y correspondencias del modelo son deducciones de este informe; no se atribuyen a OR-Tools ni a una publicación académica sobre osu!.

### Workflow, producto y pruebas

| Ref. | Fuente | Localización / evidencia utilizada |
|---|---|---|
| W1 | [Workflow: PROJECT_PROFILE.md](https://github.com/FAJIFBEFJHA/ai-assisted-development-workflow/blob/49da4c5af05826f71518b38caa303635af1fdcc5/projects/osu-hitsound-editor/PROJECT_PROFILE.md) | Plataforma, autoría, jerarquía de fuentes y separación lógica/física. |
| W2 | [Workflow: PROJECT_CONTEXT.md](https://github.com/FAJIFBEFJHA/ai-assisted-development-workflow/blob/49da4c5af05826f71518b38caa303635af1fdcc5/projects/osu-hitsound-editor/PROJECT_CONTEXT.md) | SHA del producto, 741/741 histórico, SampleId diferido, scheduler pendiente y hito pre-WPF. |
| W3 | [Workflow: AI_WORKFLOW.md](https://github.com/FAJIFBEFJHA/ai-assisted-development-workflow/blob/49da4c5af05826f71518b38caa303635af1fdcc5/projects/osu-hitsound-editor/AI_WORKFLOW.md) | Reglas de investigación, lectura vigente y límites del trabajo. |
| P1 | [docs/DECISIONS.md](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/docs/DECISIONS.md) | D002–D005, D010–D012, D016–D022, D024, D026–D027. |
| P2 | [docs/ROADMAP.md](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/docs/ROADMAP.md#L260) | Hitos pre-WPF, Export and Round-Trip Verification y Sample Optimization. |
| P14 | [docs/architecture.md](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/docs/architecture.md) | Responsabilidades, herencia, componentes de slider y límites arquitectónicos actuales. |
| P3 | [src/OsuHitsoundEditor/Beatmap.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/Beatmap.cs#L327) | Herencia 327–412; capas 415–565; eventos 640–708; timing/ticks 115–298. |
| P4 | [src/OsuHitsoundEditor/SampleResolver.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/SampleResolver.cs#L60) | Índice 0/1 y universales 60–76; custom 207–233; extensiones 235–276; resultado 279–347. |
| P5 | [src/OsuHitsoundEditor/BeatmapLoader.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/BeatmapLoader.cs#L185) | Carga parcial, timing 185–205; objetos 208–255; campos de sample 258–270. |
| P6 | [src/OsuHitsoundEditor/HitSoundLayer.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/HitSoundLayer.cs) | Propiedades resueltas de la capa; no incluye SampleId. |
| P6b | [src/OsuHitsoundEditor/TimedHitSoundEvent.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/TimedHitSoundEvent.cs) | Referencia al objeto, tiempo y EdgeIndex. Se contrastó también HitObject.cs y HitSoundEventType.cs. |
| P7 | [src/OsuHitsoundEditor/HitSoundPlaybackService.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/HitSoundPlaybackService.cs#L137) | Dispatch, recursos ausentes, PlaySliderBody, QueueActiveSliderBodiesAt, QueuePlaybackInterval. |
| P8 | [tests/.../ExplicitFilenameCompatibility/CASES.md](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/tests/OsuHitsoundEditor.Tests/TestData/ExplicitFilenameCompatibility/CASES.md) | Fixture histórica de 04-10-2026. SHA-256 del artefacto: 5a0feb3533012a0d5d913c097c3124e233fc46efc750ca7d9491bb906c40093c. |
| P8b | [tests/.../ExplicitFilenameRemainingComponents/CASES.md](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/tests/OsuHitsoundEditor.Tests/TestData/ExplicitFilenameRemainingComponents/CASES.md) | Spinner/slider. SHA-256: d23d3a64187230dc66cc14dff9559256d1ddaf4d17565b0659b9af746deea39a. La no observación de cuerpo/ticks en esta fixture no se generaliza a todos los sliders. |
| P9 | [tests/OsuHitsoundEditor.Tests/SampleResolverTests.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/tests/OsuHitsoundEditor.Tests/SampleResolverTests.cs#L1190) | PrefersSpecificBeatmapSampleOverUniversalSample y variantes de universal 1190–1408; precedencia de extensiones 650–805. |
| P10 | [tests/OsuHitsoundEditor.Tests/BeatmapTests.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/tests/OsuHitsoundEditor.Tests/BeatmapTests.cs#L1293) | UsesTimingPointAtEdgeTime, versiones/ticks, trailing fields y pre-primer punto 1913–2348; IgnoresNonDiscreteHitObjects en 3298. |
| P11 | [src/OsuHitsoundEditor/AudioPlaybackEngine.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/AudioPlaybackEngine.cs#L176) | Fuentes, volumen 216/334, delays, loop y fase. |
| P12 | [tests/OsuHitsoundEditor.Tests/HitSoundPlaybackServiceTests.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/tests/OsuHitsoundEditor.Tests/HitSoundPlaybackServiceTests.cs#L220) | PlayHitSoundLayers_WhenTwoSamplesExist_MixesIndividualVolumes: mezcla dos capas a 50/25%; no prueba exportabilidad. |
| P13 | [src/OsuHitsoundEditor/SampleResolutionResult.cs](https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/SampleResolutionResult.cs) | Outcome, ResolvedPath y FallbackFilename; contrastado con SampleResolutionOutcome.cs. |

### Documentación e implementación oficial de osu!

| Ref. | Fuente | Localización / evidencia utilizada |
|---|---|---|
| O1 | [Wiki oficial: .osu (file format)](https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29) | Timing points, Hitsounds, Custom hit samples, Sliders y Spinners; contrastar detalles legacy con código. |
| O2 | [osu.Game/Rulesets/Objects/Legacy/ConvertHitObjectParser.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Rulesets/Objects/Legacy/ConvertHitObjectParser.cs#L197) | readCustomSampleBanks; convertSoundType 543–568; LegacyHitSampleInfo 611–682; FileHitSampleInfo 685–704. |
| O3 | [osu.Game/Beatmaps/Formats/LegacyDecoder.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Beatmaps/Formats/LegacyDecoder.cs#L188) | LegacySampleControlPoint.ApplyTo: índice, volumen y banco heredados. |
| O3b | [osu.Game/Beatmaps/Legacy/LegacyControlPointInfo.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Beatmaps/Legacy/LegacyControlPointInfo.cs#L26) | SamplePointAt: fallback al primer punto o al predeterminado. |
| O4 | [osu.Game/Skinning/LegacySkin.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Skinning/LegacySkin.cs#L586) | GetSample y getLegacyLookupNames 610–633: sufijos y universal. |
| O5 | [osu.Game/Skinning/LegacyBeatmapSkin.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Skinning/LegacyBeatmapSkin.cs#L92) | Elegibilidad de samples del beatmap. |
| O5b | [osu.Game/Audio/HitSampleInfo.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Audio/HitSampleInfo.cs#L85) | LookupNames; Equals/GetHashCode no incluyen Volume: no usar esa igualdad como verificador completo. |
| O6 | [Wiki oficial: skin.ini](https://osu.ppy.sh/wiki/en/Skinning/skin.ini#general) | LayeredHitSounds y efecto sobre Normal; valor predeterminado 1. |
| O7 | [osu.Game/Skinning/LegacySkinTransformer.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Skinning/LegacySkinTransformer.cs#L25) | LayeredHitSounds e IsLayered: omisión mediante sample virtual. |
| O8 | [osu.Game.Tests/Gameplay/TestSceneHitObjectSamples.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game.Tests/Gameplay/TestSceneHitObjectSamples.cs#L118) | TestFileSampleFallsBackToNormal, bancos custom y LayeredHitSounds. |
| O9 | [osu.Game.Rulesets.Osu/Objects/Slider.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game.Rulesets.Osu/Objects/Slider.cs#L256) | UpdateNestedSamples: ticks, cabeza, repetición, cola. |
| O10 | [osu.Game.Rulesets.Osu/Objects/Drawables/DrawableSlider.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game.Rulesets.Osu/Objects/Drawables/DrawableSlider.cs#L163) | LoadSamples; loop, tracking y balance en actualización. |
| O10b | [osu.Game.Rulesets.Osu/Objects/Drawables/DrawableSpinner.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game.Rulesets.Osu/Objects/Drawables/DrawableSpinner.cs) | Spinning sample, frecuencia y bonus de lazer. |
| O11 | [osu.Game/Beatmaps/Formats/LegacyBeatmapDecoder.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Beatmaps/Formats/LegacyBeatmapDecoder.cs#L168) | applySamples 168–188 y CONTROL_POINT_LENIENCY en 39. |
| O12 | [osu.Game/Rulesets/Objects/Drawables/DrawableHitObject.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Rulesets/Objects/Drawables/DrawableHitObject.cs#L168) | Mínimo lazer 5%; comentario oficial sobre stable 8%; CalculateSamplePlaybackBalance y PlaySamples. |
| O12b | [osu.Game/Skinning/SkinnableSound.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Skinning/SkinnableSound.cs#L153) | Construcción de samples, aplicación del mínimo y reproducción de capas. |
| O13 | [Wiki oficial: Ranking criteria](https://osu.ppy.sh/wiki/en/Ranking_criteria) | Reglas de archivos y Audio / Rules / Guidelines. La recomendación sobre Finish es guía, no prohibición absoluta. |
| O14 | [Wiki oficial: osu! ranking criteria](https://osu.ppy.sh/wiki/en/Ranking_criteria/osu%21) | Overall / General / Rules: feedback audible en partes activamente clicables. |
| O15 | [Wiki oficial: Hitsound](https://osu.ppy.sh/wiki/en/Beatmapping/Hitsound) | Activo, pasivo, storyboard y keysound. |
| O15b | [Wiki oficial: Custom sample overrides](https://osu.ppy.sh/wiki/en/Guides/Custom_sample_overrides) | Nombres, loops y muestras universales. |
| O15c | [Wiki oficial: Skinnable sounds](https://osu.ppy.sh/wiki/en/Skinning/Sounds) | Nombres de hitsounds y sonidos propios de spinner. |
| O16 | [osu.Game/Beatmaps/Formats/LegacyBeatmapEncoder.cs](https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Beatmaps/Formats/LegacyBeatmapEncoder.cs#L593) | getSampleBank y controles de samples. Referencia de serialización, no prueba de optimización global ni de equivalencia arbitraria. |

### Optimización

| Ref. | Fuente | Localización / evidencia utilizada |
|---|---|---|
| A1 | [Google OR-Tools: CP-SAT Solver](https://developers.google.com/optimization/cp/cp_solver) | Dominio entero, estados del solver y ejemplos C#. |
| A2 | [Google OR-Tools: Integer Optimization](https://developers.google.com/optimization/mip) | Modelos binarios/enteros y comparación orientativa MIP/CP-SAT; no elección universal. |

### Fragmentos mínimos que sustentan puntos críticos

| Fuente | Fragmento | Interpretación acotada |
|---|---|---|
| P3, línea 419 | `hitSoundTypes.Add(HitSoundType.Normal);` | El modelo propio incluye Normal siempre. |
| P4, línea 65 | `return (null, null, fallbackFilename);` | El índice efectivo 0 evita los dos candidatos locales. |
| P5, línea 238 | `if (hitObject.IsSlider && parts.Length > 10)` | La carga actual de detalles del slider exige esos campos. |
| P11, línea 216 | `volumeProvider.Volume = volume / 100f;` | Ganancia lineal propia, sin mínimo de cliente en esa asignación. |
| O2, método readCustomSampleBanks | `if (banksOnly) return;` | Index/Volume/Filename no se leen para esa ruta del slider legacy. |
| O4, getLegacyLookupNames | `yield return hitSample.Name;` | Candidato universal después de candidatos específicos. |
| O11, línea 39 | `public const double CONTROL_POINT_LENIENCY = 5;` | Tolerancia del decoder oficial consultado; no medición nueva de stable. |

---

**Control de alcance:** informe externo a ambos repositorios; sin cambios de código, pruebas, decisiones o documentación; sin commits ni pushes. Los modelos, casos, pseudocódigo y pruebas futuras no constituyen una implementación ni autorización para iniciarla.

[W1]: https://github.com/FAJIFBEFJHA/ai-assisted-development-workflow/blob/49da4c5af05826f71518b38caa303635af1fdcc5/projects/osu-hitsound-editor/PROJECT_PROFILE.md
[W2]: https://github.com/FAJIFBEFJHA/ai-assisted-development-workflow/blob/49da4c5af05826f71518b38caa303635af1fdcc5/projects/osu-hitsound-editor/PROJECT_CONTEXT.md
[W3]: https://github.com/FAJIFBEFJHA/ai-assisted-development-workflow/blob/49da4c5af05826f71518b38caa303635af1fdcc5/projects/osu-hitsound-editor/AI_WORKFLOW.md
[P1]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/docs/DECISIONS.md
[P2]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/docs/ROADMAP.md#L260
[P14]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/docs/architecture.md
[P3]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/Beatmap.cs#L327
[P4]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/SampleResolver.cs#L60
[P5]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/BeatmapLoader.cs#L185
[P6]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/HitSoundLayer.cs
[P6b]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/TimedHitSoundEvent.cs
[P7]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/HitSoundPlaybackService.cs#L137
[P8]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/tests/OsuHitsoundEditor.Tests/TestData/ExplicitFilenameCompatibility/CASES.md
[P8b]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/tests/OsuHitsoundEditor.Tests/TestData/ExplicitFilenameRemainingComponents/CASES.md
[P9]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/tests/OsuHitsoundEditor.Tests/SampleResolverTests.cs#L1190
[P10]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/tests/OsuHitsoundEditor.Tests/BeatmapTests.cs#L1293
[P11]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/AudioPlaybackEngine.cs#L176
[P12]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/tests/OsuHitsoundEditor.Tests/HitSoundPlaybackServiceTests.cs#L220
[P13]: https://github.com/FAJIFBEFJHA/osu-hitsound-editor/blob/1c75405d3a131f367e4c9c8be692c6c012d1c7e7/src/OsuHitsoundEditor/SampleResolutionResult.cs
[O1]: https://osu.ppy.sh/wiki/en/Client/File_formats/osu_%28file_format%29
[O2]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Rulesets/Objects/Legacy/ConvertHitObjectParser.cs#L197
[O3]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Beatmaps/Formats/LegacyDecoder.cs#L188
[O3b]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Beatmaps/Legacy/LegacyControlPointInfo.cs#L26
[O4]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Skinning/LegacySkin.cs#L586
[O5]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Skinning/LegacyBeatmapSkin.cs#L92
[O5b]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Audio/HitSampleInfo.cs#L85
[O6]: https://osu.ppy.sh/wiki/en/Skinning/skin.ini#general
[O7]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Skinning/LegacySkinTransformer.cs#L25
[O8]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game.Tests/Gameplay/TestSceneHitObjectSamples.cs#L118
[O9]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game.Rulesets.Osu/Objects/Slider.cs#L256
[O10]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game.Rulesets.Osu/Objects/Drawables/DrawableSlider.cs#L163
[O10b]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game.Rulesets.Osu/Objects/Drawables/DrawableSpinner.cs
[O11]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Beatmaps/Formats/LegacyBeatmapDecoder.cs#L168
[O12]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Rulesets/Objects/Drawables/DrawableHitObject.cs#L168
[O12b]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Skinning/SkinnableSound.cs#L153
[O13]: https://osu.ppy.sh/wiki/en/Ranking_criteria
[O14]: https://osu.ppy.sh/wiki/en/Ranking_criteria/osu%21
[O15]: https://osu.ppy.sh/wiki/en/Beatmapping/Hitsound
[O15b]: https://osu.ppy.sh/wiki/en/Guides/Custom_sample_overrides
[O15c]: https://osu.ppy.sh/wiki/en/Skinning/Sounds
[O16]: https://github.com/ppy/osu/blob/131fe72a15581049401a588a13684938d4d0e0c7/osu.Game/Beatmaps/Formats/LegacyBeatmapEncoder.cs#L593
[A1]: https://developers.google.com/optimization/cp/cp_solver
[A2]: https://developers.google.com/optimization/mip
