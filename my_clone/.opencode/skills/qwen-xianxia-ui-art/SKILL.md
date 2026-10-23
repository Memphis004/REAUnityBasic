---
name: qwen-xianxia-ui-art
description: Generate hand-painted xianxia UI sprites with local Qwen Image 2.1 in ComfyUI, reference conditioning, user selection checkpoints, deterministic magenta removal, and minimal Unity integration.
---

# Qwen xianxia UI art pipeline

## Known successful example

The user explicitly loved and approved the scroll-frame redesign generated with:
- Reference: `art/reference/disciple_list_ref.png` (style/layout only; never copy pixels, text or logos).
- Workflow: `art/disciple_list_v2/redesign_probe_seed210002.json`.
- Full successful prompt: node `5`, `inputs.prompt` in that workflow. Read it rather than reconstructing it from memory.
- Seed `210002`, 1024x576, 40 steps, CFG 1, Euler / simple, denoise 1.
- Encoder reference resolution 768. Input wiring `images.image_1` into `TextEncodeQwenImage21`.
- Diffusion `qwen_image_2.1_int8_convrot.safetensors`; encoder `qwen3vl_8b_int8_convrot.safetensors`, CLIP type `qwen_image`; VAE `qwen_image_2.1_vae_bf16.safetensors`.
- No Prompt Enhancer, LoRA or paid API. The missing `qwen3.5_9b...pe_i2i` encoder in the gallery template is an OPTIONAL enhancer, not the main image encoder. Remove that branch; do not swap encoder names blindly.

Style anchors: **scroll, not a generic rectangular border**; tall muted apricot lacquered wood rollers at both sides, thin organic dark-ink outlines, engraved curls and layered end caps; broad calm ivory watercolor parchment; grey-beige rolled top/bottom edges; cream/apricot auspicious clouds confined to bottom corners. Central card space stays completely empty. Avoid glossy gold fantasy ornaments.

The first plain-rectangle smoke test did not demonstrate the desired art style. Never present a tool smoke test as finished design.

## Safety and checkpoints

1. Read actual project conventions and approved write scope. Preserve unrelated dirty changes.
2. Locate the exact reference file. If missing, ask the user for its path. Chat attachments do not automatically provide bytes usable by ComfyUI.
3. Call ComfyUI `server_info`, live node schemas and model catalog. Tell the user about outdated core/packs; do not update or restart without authorization.
4. Inspect VRAM before running. If short, request model unload using `free_memory` and remeasure. A valid workflow is not an OOM safety guarantee.
5. Upload reference without overwriting existing inputs, then use the returned filename. Validate every edited graph before running. Keep user assets/workflows within allowed project folders; ComfyUI may create operational input/output/cache files in its own configured directories.
6. No paid model, extra model download, install or broad retry loop without user consent.
7. Put prohibitions **inside the prompt**: no text, letters, numbers, logos, watermarks, characters, people, portraits, shadows, drop shadows, glow or halos. Specify no cards/buttons/medallion/title plate for frame-only art.
8. Start with ONE reference-conditioned probe. If the requested smoke-test background gate fails, STOP and report; any retry or relaxed criterion needs user approval.
9. Follow requested variant count/contact sheets and stop at user-selection Checkpoint A. Never select an asset on the user's behalf. Generate small parts using the selected frame as reference and stop at Checkpoint B.
10. A direct user request to install a loved probe selects that frame, not automatically all ungenerated small parts. State frame-only scope and retain old small parts unless the user asks otherwise.

## Magenta and deterministic processing

Qwen may output nearly magenta, not exact flat #FF00FF. The approved redesign had only 67.67% exact magenta in the top/bottom 8px strips. Do not claim a strict flat-background gate passed. Installation of this specific loved probe was authorized by the user after this limitation was reported; it is not blanket permission for future work.

Reuse `tools/art/postprocess.py`: Euclidean magenta distance LOW=60/HIGH=160, feathered alpha, edge despill, nearest-neighbor RGB color bleed, alpha trim, 4px padding, Lanczos downscale only, then bleed final hidden RGB again.

Example (run only when generation/processing is authorized):
```bash
python tools/art/postprocess.py art/disciple_list_v2/redesign_probe/3a042d8f_000.png scroll_frame --manifest art/disciple_list_v2/manifest.json --output-dir art/disciple_list_v2/final --frame-aspect 1.7721518987341771
```

For this frame, cloud shapes already touch source edges. Transparent canvas padding preserves existing pixels; it does NOT reconstruct clipped ornament. Do not crop more of the silhouette away or claim missing edges were restored. The final canvas is 1020x576, not upscaled. Record source/final aspect and method in the report.

Require RGBA, zero near-magenta RGB pixels INCLUDING alpha-zero pixels, dimensions no larger than source, alpha ratios and byte count. Test repeat processing for identical bytes. Do not substitute a higher-resolution generation for the selected seed without approval.

Measure **cream-paper bounds**, not transparent silhouette or wood. The current conservative method follows center-connected cream-color runs over central 60% scanlines, using alpha>=250, R>=205, G>=195, B>=170, RGB spread<=55, then takes the most inward boundaries. Record thresholds/coordinate origin/method. This can over-inset textured paper; user visual inspection remains required. Do not invent measurements.

## Unity integration

- Backup the CURRENT sprite, .meta, generator/importer, prefab and catalog before changes. Existing `art/disciple_list_v1_backup/` files may be older: use a new non-overwriting subfolder.
- `tools/art/install_approved_frame.py` is SPECIFIC to approved seed 210002. Do not run it for a different selection or treat its existence as user approval.
- Preserve sprite .meta GUID. Use existing resource name `scroll_frame.png`.
- Keep 1400x790 window; Image.Simple, preserveAspect=false; no extra rod/cloud children.
- Update only measured inset fractions, necessary decoration placement, and importer dimensions. Retain Button, Thai TMP label and Esc behavior. No model/message/state/Shared/Luban/MCP bridge changes.
- Keep existing single-warning fallback policy. Do not rewrite runtime layout for art changes.
- Run actual importer and generator in Unity Editor. Inspect prefab save, unique catalog entry, missing scripts and console baseline; repeat open/close three times and click a card into DiscipleDetail. Run relevant tests and compile checks.
- If Unity MCP is disconnected, report integration as **sprite installed / regeneration pending**, never claim prefab/runtime verification succeeded. Do not start another Unity on a project already open.

## Required final report

List changed files and rollback location; chosen prompt source + seed; numeric checks versus user-visible checks; total generated images and paid credits (this example 2 images, 0 API credits); pending Unity checks; suggest a separate card kit phase (tier frames, level circle and ribbon only, no portraits unless requested).
