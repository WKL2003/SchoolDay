import os
import uuid
import numpy as np
from PIL import Image
import cv2

ART_DIR = "Assets/Game/Art/Character"

META_TEMPLATE = """fileFormatVersion: 2
guid: __GUID__
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0}
  spritePixelsToUnits: 100
  spriteBorder: {x: 0, y: 0, z: 0, w: 0}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 4
    buildTarget: Standalone
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

def ensure_meta(png_path):
    meta_path = png_path + ".meta"
    guid = None
    if not os.path.exists(meta_path):
        guid = uuid.uuid4().hex
        with open(meta_path, "w") as f:
            f.write(META_TEMPLATE.replace("__GUID__", guid))
        print(f"Created meta: {meta_path} (guid: {guid})")
    else:
        # Read existing guid
        with open(meta_path, "r") as f:
            for line in f:
                if line.startswith("guid:"):
                    guid = line.split(":", 1)[1].strip()
                    break
        if not guid:
            guid = uuid.uuid4().hex
            with open(meta_path, "w") as f:
                f.write(META_TEMPLATE.replace("__GUID__", guid))
    return guid

def save_clean_rgba(img_arr, out_path):
    # img_arr is uint8 RGBA
    img_arr = img_arr.copy()
    # Crisp binary alpha: no semi-transparent fuzzy edges
    img_arr[img_arr[:, :, 3] < 128] = [0, 0, 0, 0]
    img_arr[img_arr[:, :, 3] >= 128, 3] = 255
    # Zero out RGB of fully transparent pixels
    zero_alpha = img_arr[:, :, 3] == 0
    img_arr[zero_alpha] = [0, 0, 0, 0]
    img = Image.fromarray(img_arr)
    img.save(out_path)
    ensure_meta(out_path)
    print(f"Exported: {out_path} ({img.size})")

def generate_idle_breath(src_path, y_waist, y_chest, y_hands, cx, prefix):
    src = Image.open(src_path).convert("RGBA")
    w, h = src.size
    arr = np.array(src, dtype=np.uint8)
    arr_f = arr.astype(np.float32)
    
    grid_y, grid_x = np.mgrid[0:h, 0:w].astype(np.float32)
    
    # Frame 0: Exact match to still
    save_clean_rgba(arr, f"{ART_DIR}/{prefix}_0.png")
    
    # Frames 1 and 2
    for step, (dy_max, dx_max, frame_idx) in enumerate([(4.0, 1.0, 1), (8.0, 2.0, 2)]):
        map_x = grid_x.copy()
        map_y = grid_y.copy()
        
        # Smooth vertical weight:
        # y >= y_waist: weight = 0 (completely locked)
        # y <= y_chest: weight = 1 (upper body rises)
        t = np.clip((y_waist - grid_y) / (y_waist - y_chest), 0.0, 1.0)
        smooth_t = t * t * (3.0 - 2.0 * t)
        
        # Hands dampening (arms stay mostly at sides)
        dist_x = np.abs(grid_x - cx)
        arm_t = np.clip((grid_y - y_chest) / (y_hands - y_chest), 0.0, 1.0)
        arm_weight = np.clip((dist_x - 70.0) / 60.0, 0.0, 1.0)
        vert_weight = smooth_t * (1.0 - 0.7 * arm_t * arm_weight)
        
        dy = dy_max * vert_weight
        map_y += dy
        
        # Subtle ribcage expansion
        chest_t = np.exp(-((grid_y - (y_chest + 40.0)) ** 2) / (2.0 * 45.0 ** 2))
        dx_dir = np.sign(grid_x - cx)
        dx = -dx_max * chest_t * dx_dir * np.clip(dist_x / 80.0, 0.0, 1.0)
        map_x += dx
        
        remapped = cv2.remap(arr_f, map_x, map_y, interpolation=cv2.INTER_LANCZOS4, borderMode=cv2.BORDER_CONSTANT, borderValue=(0,0,0,0))
        
        # Lower body locked bit-for-bit to Frame 0
        locked_mask = grid_y >= y_waist
        remapped[locked_mask] = arr_f[locked_mask]
        
        remapped[remapped[:, :, 3] < 2.0] = 0
        out_arr = np.clip(remapped, 0, 255).astype(np.uint8)
        save_clean_rgba(out_arr, f"{ART_DIR}/{prefix}_{frame_idx}.png")

def generate_low_energy_nod(src_path, y_chin, y_collar, prefix):
    src = Image.open(src_path).convert("RGBA")
    w, h = src.size
    arr = np.array(src, dtype=np.uint8)
    arr_f = arr.astype(np.float32)
    
    grid_y, grid_x = np.mgrid[0:h, 0:w].astype(np.float32)
    
    # Frame 0: Exact match to still
    save_clean_rgba(arr, f"{ART_DIR}/{prefix}_0.png")
    
    for step, (dy_head, frame_idx) in enumerate([(5.0, 1), (10.0, 2)]):
        map_x = grid_x.copy()
        map_y = grid_y.copy()
        
        weight = np.zeros_like(grid_y)
        head_mask = grid_y < y_chin
        weight[head_mask] = 1.0
        
        neck_mask = (grid_y >= y_chin) & (grid_y <= y_collar)
        t = np.clip((y_collar - grid_y[neck_mask]) / (y_collar - y_chin), 0.0, 1.0)
        weight[neck_mask] = t * t * (3.0 - 2.0 * t)
        
        map_y -= dy_head * weight
        
        remapped = cv2.remap(arr_f, map_x, map_y, interpolation=cv2.INTER_LANCZOS4, borderMode=cv2.BORDER_CONSTANT, borderValue=(0,0,0,0))
        
        # Body locked bit-for-bit below collar
        body_locked = grid_y >= y_collar
        remapped[body_locked] = arr_f[body_locked]
        
        remapped[remapped[:, :, 3] < 2.0] = 0
        out_arr = np.clip(remapped, 0, 255).astype(np.uint8)
        save_clean_rgba(out_arr, f"{ART_DIR}/{prefix}_{frame_idx}.png")

def generate_low_reputation_fidget(src_path, y_min, y_max, x_center, x_radius, prefix):
    src = Image.open(src_path).convert("RGBA")
    w, h = src.size
    arr = np.array(src, dtype=np.uint8)
    arr_f = arr.astype(np.float32)
    
    grid_y, grid_x = np.mgrid[0:h, 0:w].astype(np.float32)
    
    # Frame 0: Exact match to still
    save_clean_rgba(arr, f"{ART_DIR}/{prefix}_0.png")
    
    for step, (dx_move, frame_idx) in enumerate([(3.5, 1), (7.0, 2)]):
        map_x = grid_x.copy()
        map_y = grid_y.copy()
        
        y_center = (y_min + y_max) / 2.0
        y_sigma = (y_max - y_min) / 3.0
        dist_y = np.exp(-((grid_y - y_center) ** 2) / (2.0 * y_sigma ** 2))
        
        dist_x = np.abs(grid_x - x_center)
        x_weight = np.clip(1.0 - (dist_x / x_radius), 0.0, 1.0)
        x_weight = x_weight * x_weight * (3.0 - 2.0 * x_weight)
        
        total_weight = dist_y * x_weight
        direction = -np.sign(grid_x - x_center)
        
        map_x -= dx_move * total_weight * direction
        
        remapped = cv2.remap(arr_f, map_x, map_y, interpolation=cv2.INTER_LANCZOS4, borderMode=cv2.BORDER_CONSTANT, borderValue=(0,0,0,0))
        
        # Strictly lock everything outside hand area
        outside_mask = (grid_y < y_min - 10) | (grid_y > y_max + 10) | (dist_x > x_radius + 15)
        remapped[outside_mask] = arr_f[outside_mask]
        
        remapped[remapped[:, :, 3] < 2.0] = 0
        out_arr = np.clip(remapped, 0, 255).astype(np.uint8)
        save_clean_rgba(out_arr, f"{ART_DIR}/{prefix}_{frame_idx}.png")

def generate_high_energy_reputation_bounce(src_path, y_feet_bottom, y_knees, prefix):
    src = Image.open(src_path).convert("RGBA")
    w, h = src.size
    arr = np.array(src, dtype=np.uint8)
    arr_f = arr.astype(np.float32)
    
    grid_y, grid_x = np.mgrid[0:h, 0:w].astype(np.float32)
    
    # Frame 0: Exact match to still
    save_clean_rgba(arr, f"{ART_DIR}/{prefix}_0.png")
    
    for step, (dy_bounce, frame_idx) in enumerate([(4.5, 1), (9.0, 2)]):
        map_x = grid_x.copy()
        map_y = grid_y.copy()
        
        # Smooth weight from knees to feet
        weight = np.ones_like(grid_y)
        foot_zone = grid_y >= y_knees
        t = np.clip((y_feet_bottom - grid_y[foot_zone]) / (y_feet_bottom - y_knees), 0.0, 1.0)
        weight[foot_zone] = t ** 1.5
        weight[grid_y > y_feet_bottom] = 0.0
        
        map_y += dy_bounce * weight
        
        remapped = cv2.remap(arr_f, map_x, map_y, interpolation=cv2.INTER_LANCZOS4, borderMode=cv2.BORDER_CONSTANT, borderValue=(0,0,0,0))
        
        # Bottom-most row contact locked
        sole_contact = grid_y >= y_feet_bottom
        remapped[sole_contact] = arr_f[sole_contact]
        
        remapped[remapped[:, :, 3] < 2.0] = 0
        out_arr = np.clip(remapped, 0, 255).astype(np.uint8)
        save_clean_rgba(out_arr, f"{ART_DIR}/{prefix}_{frame_idx}.png")

def main():
    print("=== Generating Boy Standing Loops (1024x1024) ===")
    # 1. Boy Idle Breath
    generate_idle_breath(f"{ART_DIR}/student_idle.png", 575, 420, 650, 509.0, "student_idle")
    
    # 2. Boy Low Energy Nod
    generate_low_energy_nod(f"{ART_DIR}/student_low_energy.png", 365, 410, "student_low_energy")
    
    # 3. Boy Low Reputation Finger Fidget
    generate_low_reputation_fidget(f"{ART_DIR}/student_low_reputation.png", 430, 550, 507.0, 60.0, "student_low_reputation")
    
    # 4. Boy High Energy Rep Bounce
    generate_high_energy_reputation_bounce(f"{ART_DIR}/student_high_energy_reputation.png", 956, 750, "student_high_energy_reputation")

    print("\n=== Generating Girl Standing Loops (832x1248) ===")
    # 5. Girl Idle Breath
    generate_idle_breath(f"{ART_DIR}/student_girl_idle.png", 645, 440, 750, 414.0, "student_girl_idle")
    
    # 6. Girl Low Energy Nod
    generate_low_energy_nod(f"{ART_DIR}/student_girl_low_energy.png", 400, 440, "student_girl_low_energy")
    
    # 7. Girl Low Reputation Finger Fidget
    generate_low_reputation_fidget(f"{ART_DIR}/student_girl_low_reputation.png", 500, 645, 414.0, 65.0, "student_girl_low_reputation")
    
    # 8. Girl High Energy Rep Bounce
    generate_high_energy_reputation_bounce(f"{ART_DIR}/student_girl_high_energy_reputation.png", 1132, 880, "student_girl_high_energy_reputation")

    print("\n=== Creating Preview Contact Sheets & Ping-Pong GIFs ===")
    loops = [
        ("student_idle", (1024, 1024), "Boy Idle (Breath)"),
        ("student_low_energy", (1024, 1024), "Boy Low Energy (Head Nod)"),
        ("student_low_reputation", (1024, 1024), "Boy Low Reputation (Finger Fidget)"),
        ("student_high_energy_reputation", (1024, 1024), "Boy High Energy Rep (Bounce)"),
        ("student_girl_idle", (832, 1248), "Girl Idle (Breath)"),
        ("student_girl_low_energy", (832, 1248), "Girl Low Energy (Head Nod)"),
        ("student_girl_low_reputation", (832, 1248), "Girl Low Reputation (Finger Fidget)"),
        ("student_girl_high_energy_reputation", (832, 1248), "Girl High Energy Rep (Bounce)"),
    ]
    
    for prefix, size, desc in loops:
        f0 = Image.open(f"{ART_DIR}/{prefix}_0.png")
        f1 = Image.open(f"{ART_DIR}/{prefix}_1.png")
        f2 = Image.open(f"{ART_DIR}/{prefix}_2.png")
        
        # Ping-pong loop: 0 -> 1 -> 2 -> 1 -> 0
        gif_frames = [f0, f1, f2, f1]
        gif_path = f"preview_{prefix}_loop.gif"
        gif_frames[0].save(gif_path, save_all=True, append_images=gif_frames[1:], duration=250, loop=0)
        print(f"Generated GIF: {gif_path}")

    # Build Contact Sheet (8 rows x 3 columns)
    # Thumbnail width: 256
    thumb_w = 200
    sheet_w = thumb_w * 3 + 40
    total_h = 0
    row_heights = []
    for prefix, (w, h), desc in loops:
        thumb_h = int(round(thumb_w * (h / w)))
        row_heights.append(thumb_h)
        total_h += thumb_h + 30
    
    sheet = Image.new("RGBA", (sheet_w, total_h + 20), (28, 32, 38, 255))
    curr_y = 20
    for idx, (prefix, (w, h), desc) in enumerate(loops):
        th = row_heights[idx]
        for col in range(3):
            im = Image.open(f"{ART_DIR}/{prefix}_{col}.png")
            thumb = im.resize((thumb_w, th), Image.Resampling.LANCZOS)
            x_pos = 20 + col * (thumb_w + 10)
            sheet.paste(thumb, (x_pos, curr_y), thumb)
        curr_y += th + 30
    
    sheet.save("contact_sheet_all_24_frames.png")
    print("Exported master contact sheet: contact_sheet_all_24_frames.png")

if __name__ == "__main__":
    main()
