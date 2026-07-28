# Taller del robot — nivel 3

Esta carpeta agrupa las fuentes editables y la información de los modelos
empleados por la práctica de encapsulamiento.

## Estructura

- `Source/Robot/copiaRobotRemplazo.blend`: fuente editable del robot y su rig.
- `Source/Temperature/Temperatura.blend`: fuente editable del módulo de
  temperatura instalado en el compartimiento frontal.
- Los FBX usados en tiempo de ejecución están bajo
  `Assets/__Algolab/Resources/Level3/RobotWorkshop/Models`.

## Restricciones del rig

- `Arm.L` y `Arm.R`: giro completo únicamente sobre el eje local X.
- `head`: giro completo únicamente sobre el eje local Y.
- `Leg.L` y `Leg.R`: eje local X limitado de -40 a 40 grados.
- `Torso`: rotación bloqueada.

Unity vuelve a aplicar estas reglas mediante
`AlgoLabRobotRigAxisConstraint`, porque las restricciones de Blender no se
convierten siempre en restricciones activas al importar un FBX.

## Batería

Modelo `Battery` de Quaternius, descargado de:

https://poly.pizza/m/MYa3uWdwPU

Licencia: Creative Commons CC0 1.0 (dominio público).

https://creativecommons.org/publicdomain/zero/1.0/

## Módulo de temperatura

El FBX de ejecución se genera desde `Temperatura.blend` con
`Assets/__Algolab/Editor/AlgoLabExportTemperatureModule.py`. En la práctica
queda instalado detrás del vidrio frontal y solo puede agarrarse a muy corta
distancia después de romper ese vidrio.
