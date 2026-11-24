// ピクあ！共有ステージ。POST /v1/files で .piqua を受け、GET/HEAD /v1/files/:fileId で返す。
// 秘密鍵は使わない。R2 は SHARE_BUCKET バインドだけ。

const MAX_ZIP = 1024 * 1024;
const MAX_UNCOMPRESSED = 2 * 1024 * 1024;
const MAX_PNG = 256 * 1024;
const MAX_ENTRIES = 16;
const MAX_RATIO = 100;
const DAILY_UPLOADS = 30;
const FORMAT_VERSION = 1;
const GAME_VERSION = "0.0.9";
const STAT_MAX = 1000000000000;
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/;
const SIZES = [8, 16, 32, 64];
const BACKGROUNDS = ["", "kougisitu", "nekohatakouen", "chigusa_room", "moa_room", "souga_room"];
const ATTRIBUTES = ["None", "Fire", "Grass", "Water", "Light", "Dark"];

export default {
  async fetch(request, env) {
    const url = new URL(request.url);
    if (request.method === "OPTIONS")
      return cors(new Response(null, { status: 204 }));
    if (url.pathname === "/v1/files" && request.method === "POST")
      return cors(await upload(request, env));
    const match = url.pathname.match(/^\/v1\/files\/([0-9a-f-]{36})$/);
    if (match && (request.method === "GET" || request.method === "HEAD"))
      return cors(await download(request, env, match[1]));
    return cors(json(404, "ファイルが見つかりません。"));
  }
};

async function upload(request, env) {
  if (!env.SHARE_BUCKET)
    return json(500, "サーバーの保存先が未設定です。");
  const declared = Number(request.headers.get("content-length") || "0");
  if (declared > MAX_ZIP)
    return json(413, "ファイル容量を超えています。");
  const limited = await limit(request, env);
  if (limited)
    return limited;
  const body = new Uint8Array(await request.arrayBuffer());
  if (body.length < 22 || body.length > MAX_ZIP)
    return json(413, "ファイル容量を超えています。");
  let files;
  try {
    files = await unzip(body);
  } catch (error) {
    return json(400, "対応していないファイル形式です。");
  }
  let document;
  try {
    document = await validate(files);
  } catch (error) {
    return json(400, error.message || "対応していないファイル形式です。");
  }
  const key = "files/" + document.fileId + ".piqua";
  const existing = await env.SHARE_BUCKET.head(key);
  if (existing) {
    const storedHash = existing.customMetadata && (existing.customMetadata.contentHash || existing.customMetadata.contenthash);
    const same = storedHash === document.contentHash;
    if (!same)
      return json(409, "同じファイルIDは上書きできません。");
    return json(200, "ok", { fileId: document.fileId });
  }
  await env.SHARE_BUCKET.put(key, body, {
    httpMetadata: {
      contentType: "application/octet-stream",
      contentDisposition: "attachment; filename=\"" + document.fileId + ".piqua\"; hash=" + document.contentHash
    },
    customMetadata: {
      stageId: document.stageId,
      authorId: document.authorId,
      revision: String(document.revision),
      updatedAt: document.updatedAt,
      contentHash: document.contentHash
    }
  });
  return json(201, "ok", { fileId: document.fileId });
}

async function download(request, env, fileId) {
  if (!UUID.test(fileId))
    return json(400, "URLが正しくありません。");
  if (!env.SHARE_BUCKET)
    return json(500, "サーバーの保存先が未設定です。");
  const key = "files/" + fileId + ".piqua";
  if (request.method === "HEAD") {
    const head = await env.SHARE_BUCKET.head(key);
    if (!head)
      return new Response(null, { status: 404 });
    return new Response(null, {
      status: 200,
      headers: { "Content-Type": "application/octet-stream", "Content-Length": String(head.size) }
    });
  }
  const object = await env.SHARE_BUCKET.get(key);
  if (!object)
    return json(404, "ファイルが見つかりません。");
  return new Response(object.body, {
    status: 200,
    headers: {
      "Content-Type": "application/octet-stream",
      "Content-Disposition": "attachment; filename=\"" + fileId + ".piqua\""
    }
  });
}

async function limit(request, env) {
  const ip = request.headers.get("CF-Connecting-IP") || "unknown";
  const day = new Date().toISOString().slice(0, 10);
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(ip));
  const hash = hex(new Uint8Array(digest)).slice(0, 16);
  const key = "rate/" + day + "/" + hash;
  const current = await env.SHARE_BUCKET.get(key);
  const parsed = current ? Number(await current.text()) : 0;
  const count = Number.isFinite(parsed) ? parsed : 0;
  if (count >= DAILY_UPLOADS)
    return json(429, "きょうのアップロード回数を超えています。");
  await env.SHARE_BUCKET.put(key, String(count + 1));
  return null;
}

async function validate(files) {
  const names = Object.keys(files);
  if (!files["manifest.json"] || !files["stage.json"])
    throw new Error("対応していないファイル形式です。");
  let manifest;
  let stage;
  try {
    manifest = JSON.parse(text(files["manifest.json"]));
    stage = JSON.parse(text(files["stage.json"]));
  } catch (error) {
    throw new Error("ファイルが破損しています。");
  }
  if (!manifest || !stage || typeof manifest !== "object" || typeof stage !== "object")
    throw new Error("ファイルが破損しています。");
  if (manifest.formatVersion !== FORMAT_VERSION)
    throw new Error("対応していないファイル形式です。");
  if (compareVersion(manifest.gameVersion, GAME_VERSION) > 0)
    throw new Error("アプリのバージョンが古いため、このステージを読めません。");
  if (!UUID.test(manifest.fileId || "") || !UUID.test(manifest.stageId || "") || !UUID.test(manifest.authorId || ""))
    throw new Error("対応していないファイル形式です。");
  if (!Number.isInteger(manifest.revision) || manifest.revision < 1 || manifest.revision > 1000000)
    throw new Error("対応していないファイル形式です。");
  if (!instant(manifest.createdAt) || !instant(manifest.updatedAt) || manifest.updatedAt < manifest.createdAt)
    throw new Error("ファイルが破損しています。");
  if (typeof manifest.contentHash !== "string" || !/^[0-9a-f]{64}$/.test(manifest.contentHash))
    throw new Error("ファイルが破損しています。");
  if (!plain(manifest.authorName, 16, true))
    throw new Error("対応していないファイル形式です。");
  if (!plain(stage.name, 20, false) || !plain(stage.comment, 80, true))
    throw new Error("対応していないファイル形式です。");
  if (BACKGROUNDS.indexOf(stage.background) < 0 || stage.floorCount !== 300)
    throw new Error("対応していないファイル形式です。");
  const color = stage.mainColor || {};
  if (!byte(color.r) || !byte(color.g) || !byte(color.b))
    throw new Error("不正なキャラクターデータです。");
  if (!Array.isArray(stage.characters) || stage.characters.length < 1 || stage.characters.length > 9)
    throw new Error("不正なキャラクターデータです。");
  const used = { "manifest.json": true, "stage.json": true };
  const ordered = stage.characters.slice().sort((a, b) => a.slot - b.slot);
  for (let i = 0; i < ordered.length; i++) {
    const character = ordered[i];
    const file = "characters/character_" + String(i + 1).padStart(3, "0") + ".png";
    if (!character || character.slot !== i + 1 || character.file !== file)
      throw new Error("不正なキャラクターデータです。");
    if (!plain(character.name, 20, false) || ATTRIBUTES.indexOf(character.attribute) < 0)
      throw new Error("不正なキャラクターデータです。");
    if (SIZES.indexOf(character.size) < 0)
      throw new Error("不正なキャラクターデータです。");
    if (!Number.isInteger(character.level) || character.level < 1 || character.level > 9999)
      throw new Error("不正なキャラクターデータです。");
    if (!Number.isInteger(character.fusionCount) || character.fusionCount < 0 || character.fusionCount > 9999)
      throw new Error("不正なキャラクターデータです。");
    if (!Number.isInteger(character.spd) || character.spd < 0 || character.spd > 255)
      throw new Error("不正なキャラクターデータです。");
    if (!stat(character.hp) || !stat(character.atk) || !stat(character.def) || !stat(character.luc) || !stat(character.obs))
      throw new Error("不正なキャラクターデータです。");
    const png = files[file];
    if (!pngLooks(png, character.size))
      throw new Error("不正なキャラクターデータです。");
    used[file] = true;
  }
  for (let i = 0; i < names.length; i++) {
    if (!used[names[i]])
      throw new Error("対応していないファイル形式です。");
  }
  const hash = contentHash(stage, files);
  if (hash !== manifest.contentHash)
    throw new Error("ファイルが破損しています。");
  return manifest;
}

async function contentHash(stage, files) {
  const ordered = stage.characters.slice().sort((a, b) => a.slot - b.slot);
  const parts = [new TextEncoder().encode(stageJson(stage, ordered))];
  for (let i = 0; i < ordered.length; i++) {
    parts.push(new TextEncoder().encode(ordered[i].file + "\n"));
    parts.push(files[ordered[i].file]);
  }
  const digest = await crypto.subtle.digest("SHA-256", concat(parts));
  return hex(new Uint8Array(digest));
}

function stageJson(stage, ordered) {
  let json = "{\"background\":" + quote(stage.background) + ",\"characters\":[";
  for (let i = 0; i < ordered.length; i++) {
    if (i > 0)
      json += ",";
    const c = ordered[i];
    json += "{\"atk\":" + c.atk + ",\"attribute\":" + quote(c.attribute) + ",\"def\":" + c.def
      + ",\"file\":" + quote(c.file) + ",\"fusionCount\":" + c.fusionCount + ",\"hp\":" + c.hp
      + ",\"level\":" + c.level + ",\"luc\":" + c.luc + ",\"name\":" + quote(c.name)
      + ",\"obs\":" + c.obs + ",\"size\":" + c.size + ",\"slot\":" + c.slot + ",\"spd\":" + c.spd + "}";
  }
  json += "],\"comment\":" + quote(stage.comment) + ",\"floorCount\":" + stage.floorCount
    + ",\"mainColor\":{\"b\":" + stage.mainColor.b + ",\"g\":" + stage.mainColor.g + ",\"r\":" + stage.mainColor.r
    + "},\"name\":" + quote(stage.name) + "}";
  return json;
}

function quote(text) {
  let json = "\"";
  const value = text == null ? "" : String(text);
  for (let i = 0; i < value.length; i++) {
    const code = value.charCodeAt(i);
    if (value[i] === "\"" || value[i] === "\\")
      json += "\\" + value[i];
    else if (code < 32)
      json += "\\u" + code.toString(16).padStart(4, "0");
    else
      json += value[i];
  }
  return json + "\"";
}

async function unzip(bytes) {
  const eocd = findEocd(bytes);
  const count = u16(bytes, eocd + 10);
  if (count < 2 || count > MAX_ENTRIES)
    throw new Error("entries");
  let offset = u32(bytes, eocd + 16);
  const files = {};
  let total = 0;
  for (let i = 0; i < count; i++) {
    if (u32(bytes, offset) !== 0x02014b50)
      throw new Error("central");
    const flags = u16(bytes, offset + 8);
    const method = u16(bytes, offset + 10);
    const compressed = u32(bytes, offset + 20);
    const uncompressed = u32(bytes, offset + 24);
    const nameLength = u16(bytes, offset + 28);
    const extraLength = u16(bytes, offset + 30);
    const commentLength = u16(bytes, offset + 32);
    const local = u32(bytes, offset + 42);
    const name = text(bytes.subarray(offset + 46, offset + 46 + nameLength));
    offset += 46 + nameLength + extraLength + commentLength;
    if ((flags & 1) !== 0 || (flags & 8) !== 0 || name.indexOf("..") >= 0 || name.indexOf("\\") >= 0 || name.startsWith("/"))
      throw new Error("path");
    if (method !== 0 && method !== 8)
      throw new Error("method");
    if (uncompressed > MAX_UNCOMPRESSED || compressed > MAX_ZIP)
      throw new Error("size");
    if (compressed > 0 && uncompressed / compressed > MAX_RATIO)
      throw new Error("ratio");
    if (u32(bytes, local) !== 0x04034b50)
      throw new Error("local");
    const localName = u16(bytes, local + 26);
    const localExtra = u16(bytes, local + 28);
    const start = local + 30 + localName + localExtra;
    const data = bytes.subarray(start, start + compressed);
    const body = method === 0 ? data : await inflate(data, uncompressed);
    if (body.length !== uncompressed)
      throw new Error("length");
    total += body.length;
    if (total > MAX_UNCOMPRESSED)
      throw new Error("total");
    if (files[name])
      throw new Error("duplicate");
    files[name] = body;
  }
  return files;
}

async function inflate(data, expected) {
  const stream = new Blob([data]).stream().pipeThrough(new DecompressionStream("deflate-raw"));
  const reader = stream.getReader();
  const chunks = [];
  let total = 0;
  while (true) {
    const step = await reader.read();
    if (step.done)
      break;
    total += step.value.length;
    if (total > expected || total > MAX_UNCOMPRESSED)
      throw new Error("bomb");
    chunks.push(step.value);
  }
  return concat(chunks);
}

function findEocd(bytes) {
  const start = Math.max(0, bytes.length - 65557);
  for (let i = bytes.length - 22; i >= start; i--) {
    if (bytes[i] === 0x50 && bytes[i + 1] === 0x4b && bytes[i + 2] === 0x05 && bytes[i + 3] === 0x06)
      return i;
  }
  throw new Error("eocd");
}

function pngLooks(png, size) {
  if (!png || png.length < 24 || png.length > MAX_PNG || SIZES.indexOf(size) < 0)
    return false;
  if (png[0] !== 137 || png[1] !== 80 || png[2] !== 78 || png[3] !== 71)
    return false;
  const width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
  const height = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
  return width === size && height === size;
}

function plain(value, max, allowEmpty) {
  if (typeof value !== "string" || value.length > max)
    return false;
  if (!allowEmpty && value.length === 0)
    return false;
  for (let i = 0; i < value.length; i++) {
    if (value.charCodeAt(i) < 32)
      return false;
  }
  return true;
}

function byte(value) {
  return Number.isInteger(value) && value >= 0 && value <= 255;
}

function stat(value) {
  return Number.isInteger(value) && value >= 0 && value <= STAT_MAX;
}

function instant(value) {
  return typeof value === "string" && /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z$/.test(value);
}

function compareVersion(left, right) {
  const a = String(left || "").split(".");
  const b = String(right || "").split(".");
  const n = Math.max(a.length, b.length);
  for (let i = 0; i < n; i++) {
    const x = Number(a[i] || 0);
    const y = Number(b[i] || 0);
    if (x !== y)
      return x > y ? 1 : -1;
  }
  return 0;
}

function text(bytes) {
  return new TextDecoder("utf-8", { fatal: true }).decode(bytes);
}

function u16(bytes, offset) {
  return bytes[offset] | (bytes[offset + 1] << 8);
}

function u32(bytes, offset) {
  return (bytes[offset] | (bytes[offset + 1] << 8) | (bytes[offset + 2] << 16) | (bytes[offset + 3] << 24)) >>> 0;
}

function concat(parts) {
  let length = 0;
  for (let i = 0; i < parts.length; i++)
    length += parts[i].length;
  const out = new Uint8Array(length);
  let offset = 0;
  for (let i = 0; i < parts.length; i++) {
    out.set(parts[i], offset);
    offset += parts[i].length;
  }
  return out;
}

function hex(bytes) {
  let text = "";
  for (let i = 0; i < bytes.length; i++)
    text += bytes[i].toString(16).padStart(2, "0");
  return text;
}

function json(status, message, extra) {
  const body = extra ? Object.assign({ error: message }, extra) : { error: message };
  if (status >= 200 && status < 300 && message === "ok")
    body.ok = true;
  return new Response(JSON.stringify(body), {
    status: status,
    headers: { "Content-Type": "application/json; charset=utf-8" }
  });
}

function cors(response) {
  const headers = new Headers(response.headers);
  headers.set("Access-Control-Allow-Origin", "*");
  headers.set("Access-Control-Allow-Methods", "GET, HEAD, POST, OPTIONS");
  headers.set("Access-Control-Allow-Headers", "Content-Type");
  return new Response(response.body, { status: response.status, headers: headers });
}
