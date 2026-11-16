# Furat — character asset specification

This document is the production handoff for the first real hero GLB asset in **ASHES OF MESOPOTAMIA**. It describes the target asset; it does not claim that a finished model exists.

## Visual identity
- Iraqi male protagonist named Furat.
- Historically inspired Iraqi clothing silhouette: dishdasha as the base garment, a traditional head wrap, agal, and a dark outer bisht.
- Ground the design in Mesopotamian/Iraqi visual references. Avoid generic European knight armor, fantasy horns, oversized pauldrons, or a generic desert nomad costume.
- The face and silhouette must remain readable at gameplay camera distance. Cloth layers should have believable thickness and restrained ornamentation.
- Create a game-ready, realistic PBR character, not a 2D illustration or a static sculpt.

## Required deliverable
- One binary glTF 2.0 .glb file with geometry, materials, textures, skeleton, and animation clips embedded.
- The asset must be self-contained: no external .bin, image, or texture URI dependencies.
- Use triangle primitives (mode: 4) and a single embedded buffer, matching the current runtime parser.
- Include POSITION, NORMAL, TEXCOORD_0, JOINTS_0, and WEIGHTS_0 on every skinned mesh primitive.
- JOINTS_0 must use unsigned byte or unsigned short values; WEIGHTS_0 must be FLOAT or normalized unsigned byte/short.
- Every skinned mesh node must reference a valid skin. Skin joints must be valid nodes and inverse bind matrices must be FLOAT MAT4 when provided.
- Include embedded base-colour textures where needed. Normal maps are supported and should use TEXCOORD_0.
- Keep material count and texture sizes appropriate for mobile hardware. Prefer a small number of texture atlases over many material slots.

## Skeleton and animation
Use one consistent humanoid skeleton with a clear root/pelvis, spine, neck, head, shoulders, upper/lower arms, hands, upper/lower legs, and feet. Keep bone names stable across future player animation clips.

Required clips, with clear names:
1. idle — relaxed breathing and subtle cloth motion.
2. walk — grounded locomotion.
3. run — faster locomotion.
4. dodge — short evasive movement.
5. attack — one-handed sword strike.
6. heavy — committed heavy strike.
7. hurt — brief hit reaction.
8. death — final fall / collapse.

Use in-place locomotion unless the clip is explicitly authored to translate the root. Keep the feet planted during idle and avoid severe foot sliding. Attack clips must leave the weapon hand readable and avoid cloth/weapon intersections.

## Scale and orientation
- Author in metres, humanoid height approximately 1.7–1.9 m.
- Character faces the runtime's forward axis consistently; do not bake arbitrary scene rotations into the mesh.
- Put the origin at the character's ground contact/root position.
- Apply transforms before export; avoid negative scale.
- Keep mesh and skeleton centred around the root and ensure the bind pose is neutral and stable.

## Runtime acceptance checks
The repository inspector is a first-pass structural check, not a substitute for visual review. A candidate should:
- parse as GLB 2.0 with embedded resources;
- contain renderable mesh primitives;
- include skinned primitives attached to skin instances;
- include animation clips targeting the skin's joint nodes;
- include UVs and normals on all visible character primitives;
- load without WebGL errors and render at the correct scale/orientation;
- demonstrate idle, walk, run, dodge, attack, heavy, hurt, and death without broken joints, severe clipping, or foot sliding.

## Delivery and review
Name the first candidate furat_character_v01.glb and place it under assets/characters/. Do not register it in the live model catalog until it passes structural checks and a visual in-game review. Keep the original source project and export settings so the asset can be revised without rebuilding it from scratch.

## Current status
No finished Furat GLB is included by this specification. The game still uses its existing procedural hero until a real asset is supplied, imported, and visually approved.
