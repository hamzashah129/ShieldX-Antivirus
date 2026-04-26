#!/usr/bin/env python3
"""
ShieldX Antivirus - Professional ICO Icon Generator
Copyright © 2024 SYED HAMZA ALI SHAH. All Rights Reserved.
"""

from PIL import Image, ImageDraw, ImageFont
import struct, io, math, os

def draw_shield_icon(size):
    """Draw a professional hexagonal shield icon."""
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    cx, cy = size // 2, size // 2
    s = size

    # ── Outer glow ring ─────────────────────────────────────────────
    for r in range(4, 0, -1):
        alpha = 30 + r * 10
        glow_r = int(s * 0.46) + r * 2
        draw.ellipse(
            [cx - glow_r, cy - glow_r, cx + glow_r, cy + glow_r],
            fill=(0, 200, 220, alpha)
        )

    # ── Dark background circle ───────────────────────────────────────
    bg_r = int(s * 0.46)
    draw.ellipse(
        [cx - bg_r, cy - bg_r, cx + bg_r, cy + bg_r],
        fill=(8, 11, 22, 255)
    )

    # ── Gradient shield polygon ──────────────────────────────────────
    shield_w = s * 0.58
    shield_h = s * 0.72
    sx = cx - shield_w / 2
    sy = cy - shield_h / 2

    # Shield path (classic shield shape)
    def shield_points(scale=1.0, ox=0, oy=0):
        sw = shield_w * scale
        sh = shield_h * scale
        bx = cx - sw/2 + ox
        by = cy - sh/2 + oy
        rx = bx + sw
        by2 = by + sh
        mid = bx + sw/2
        corner = sh * 0.25
        tip_y = by2
        return [
            (bx + corner, by),            # top-left
            (rx - corner, by),            # top-right
            (rx, by + corner),            # right-upper
            (rx, by + sh * 0.6),          # right-lower
            (mid, tip_y),                 # bottom tip
            (bx, by + sh * 0.6),          # left-lower
            (bx, by + corner),            # left-upper
        ]

    # Draw filled shield - outer (cyan gradient effect via layers)
    colors = [
        (0, 180, 220, 255),
        (0, 160, 200, 255),
        (0, 140, 180, 255),
    ]
    for i, color in enumerate(colors):
        scale = 1.0 - i * 0.04
        pts = shield_points(scale)
        draw.polygon(pts, fill=color)

    # Inner shield (dark fill)
    inner = shield_points(0.82)
    draw.polygon(inner, fill=(10, 18, 40, 255))

    # ── Cyan border on shield ────────────────────────────────────────
    for thickness in range(3, 0, -1):
        pts = shield_points(1.0 - (3 - thickness) * 0.02)
        draw.polygon(pts, outline=(0, 220, 230, 200 - thickness * 30))

    # ── "X" letter in center ─────────────────────────────────────────
    lw = max(2, s // 18)
    span = s * 0.18
    x1, y1 = cx - span, cy - span
    x2, y2 = cx + span, cy + span

    # Shadow
    for dx in range(-2, 3):
        for dy in range(-2, 3):
            if dx == 0 and dy == 0: continue
            draw.line([(x1+dx, y1+dy), (x2+dx, y2+dy)], fill=(0, 50, 80, 60), width=lw)
            draw.line([(x2+dx, y1+dy), (x1+dx, y2+dy)], fill=(0, 50, 80, 60), width=lw)

    # Main X
    draw.line([(x1, y1), (x2, y2)], fill=(0, 230, 220, 255), width=lw)
    draw.line([(x2, y1), (x1, y2)], fill=(0, 230, 220, 255), width=lw)

    # Center dot
    dot_r = max(2, s // 24)
    draw.ellipse(
        [cx - dot_r, cy - dot_r, cx + dot_r, cy + dot_r],
        fill=(255, 255, 255, 220)
    )

    # ── Small "S" badge at bottom right ─────────────────────────────
    if size >= 48:
        badge_r = int(s * 0.16)
        bx = cx + int(s * 0.22)
        by = cy + int(s * 0.20)
        draw.ellipse([bx - badge_r, by - badge_r, bx + badge_r, by + badge_r],
                     fill=(0, 122, 255, 255))
        draw.ellipse([bx - badge_r, by - badge_r, bx + badge_r, by + badge_r],
                     outline=(0, 180, 255, 255), width=max(1, s//48))
        try:
            font_size = max(8, badge_r)
            font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", font_size)
        except:
            font = ImageFont.load_default()
        draw.text((bx, by), "S", fill=(255,255,255,255), font=font, anchor="mm")

    return img


def make_ico(output_path):
    """Generate a multi-size professional ICO file with high-resolution support."""
    sizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256]
    images = []

    for sz in sizes:
        img = draw_shield_icon(sz)
        images.append((sz, img))
        print(f"  Generated {sz}x{sz} icon layer")

    # Save as ICO with all sizes using a single high-resolution source image
    base_img = images[-1][1]
    base_img.save(
        output_path,
        format="ICO",
        sizes=[(sz, sz) for sz in sizes]
    )
    print(f"\nShieldX ICO written to: {output_path}")

    # Also save a PNG preview at 256px
    preview_path = output_path.replace(".ico", "_preview.png")
    images[-1][1].save(preview_path, format="PNG")
    print(f"PNG preview: {preview_path}")

    return output_path


if __name__ == "__main__":
    # Get the directory where this script is located
    script_dir = os.path.dirname(os.path.abspath(__file__))
    project_root = os.path.dirname(script_dir)
    assets_dir = os.path.join(project_root, "assets")
    os.makedirs(assets_dir, exist_ok=True)
    output_path = os.path.join(assets_dir, "shieldx.ico")
    make_ico(output_path)
