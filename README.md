# arpg

## Architecture Notes

- We are moving toward fail-fast boundaries: scene and script contracts should be validated when the UI or gameplay system is built, not discovered later during a shared per-frame update path.
- Some structural cleanup is still needed across the project to fully support that approach. In a few places, UI refresh code still runs on the same execution path as core gameplay systems, which means a bad scene contract can interrupt unrelated gameplay behavior.
