package com.idlecolorsrpg.sharestage;

import android.app.Activity;
import android.app.Fragment;
import android.content.Intent;
import android.net.Uri;
import android.os.Handler;
import android.os.Looper;
import androidx.core.content.FileProvider;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.InputStream;
import java.io.OutputStream;

public class ShareStagePlugin {
    public static void pick(String receiverName) {
        open(receiverName, false, null, null);
    }

    public static void save(String receiverName, String cachePath, String displayName) {
        open(receiverName, true, cachePath, displayName);
    }

    public static void share(String text, String imagePath) {
        Activity activity = UnityPlayer.currentActivity;
        try {
            Intent send = new Intent(Intent.ACTION_SEND);
            send.putExtra(Intent.EXTRA_TEXT, text == null ? "" : text);
            File image = imagePath == null || imagePath.length() == 0 ? null : new File(imagePath);
            if (image != null && image.exists()) {
                Uri uri = FileProvider.getUriForFile(activity, "com.idlecolorsrpg.sharestage.fileprovider", image);
                send.setType("image/png");
                send.putExtra(Intent.EXTRA_STREAM, uri);
                send.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            } else {
                send.setType("text/plain");
            }
            activity.startActivity(Intent.createChooser(send, "共有"));
        } catch (Exception exception) {
            Intent send = new Intent(Intent.ACTION_SEND);
            send.setType("text/plain");
            send.putExtra(Intent.EXTRA_TEXT, text == null ? "" : text);
            activity.startActivity(Intent.createChooser(send, "共有"));
        }
    }

    static void open(String receiverName, boolean saving, String cachePath, String displayName) {
        try {
            Activity activity = UnityPlayer.currentActivity;
            ShareFragment fragment = (ShareFragment) activity.getFragmentManager().findFragmentByTag("ShareStageFragment");
            if (fragment == null) {
                fragment = new ShareFragment();
                activity.getFragmentManager().beginTransaction().add(fragment, "ShareStageFragment").commitAllowingStateLoss();
                activity.getFragmentManager().executePendingTransactions();
            }
            if (saving)
                fragment.save(receiverName, cachePath, displayName);
            else
                fragment.pick(receiverName);
        } catch (Exception exception) {
            UnityPlayer.UnitySendMessage(receiverName, "OnPickedPath", "");
        }
    }

    public static class ShareFragment extends Fragment {
        private String receiverName = "ShareStageFilesReceiver";
        private boolean pending;
        private boolean saving;
        private String cachePath;
        private String displayName;
        private static final int REQUEST_PICK = 62031;
        private static final int REQUEST_SAVE = 62032;

        public void pick(String name) {
            receiverName = name;
            saving = false;
            pending = true;
            if (isResumed())
                launch();
        }

        public void save(String name, String path, String title) {
            receiverName = name;
            saving = true;
            cachePath = path;
            displayName = title;
            pending = true;
            if (isResumed())
                launch();
        }

        @Override
        public void onResume() {
            super.onResume();
            if (pending)
                launch();
        }

        private void launch() {
            pending = false;
            new Handler(Looper.getMainLooper()).postDelayed(new Runnable() {
                @Override
                public void run() {
                    if (!isAdded()) {
                        pending = true;
                        return;
                    }
                    try {
                        if (saving)
                            startActivityForResult(saveIntent(), REQUEST_SAVE);
                        else
                            startActivityForResult(pickIntent(), REQUEST_PICK);
                    } catch (Exception exception) {
                        UnityPlayer.UnitySendMessage(receiverName, "OnPickedPath", "");
                    }
                }
            }, 400);
        }

        private Intent pickIntent() {
            Intent intent = new Intent(Intent.ACTION_OPEN_DOCUMENT);
            intent.addCategory(Intent.CATEGORY_OPENABLE);
            intent.setType("*/*");
            intent.putExtra(Intent.EXTRA_MIME_TYPES, new String[] {"application/octet-stream", "application/zip", "*/*"});
            intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            return intent;
        }

        private Intent saveIntent() {
            Intent intent = new Intent(Intent.ACTION_CREATE_DOCUMENT);
            intent.addCategory(Intent.CATEGORY_OPENABLE);
            intent.setType("application/octet-stream");
            intent.putExtra(Intent.EXTRA_TITLE, displayName == null ? "stage.piqua" : displayName);
            return intent;
        }

        @Override
        public void onActivityResult(int requestCode, int resultCode, Intent data) {
            super.onActivityResult(requestCode, resultCode, data);
            if (requestCode != REQUEST_PICK && requestCode != REQUEST_SAVE)
                return;
            if (resultCode != Activity.RESULT_OK || data == null || data.getData() == null) {
                UnityPlayer.UnitySendMessage(receiverName, "OnPickedPath", "");
                return;
            }
            String path = requestCode == REQUEST_SAVE ? writeToUri(data.getData()) : copyToCache(data.getData());
            UnityPlayer.UnitySendMessage(receiverName, "OnPickedPath", path == null ? "" : path);
        }

        private String copyToCache(Uri uri) {
            InputStream input = null;
            FileOutputStream output = null;
            try {
                input = getActivity().getContentResolver().openInputStream(uri);
                if (input == null)
                    return null;
                File dir = new File(getActivity().getCacheDir(), "sharestage");
                if (!dir.exists())
                    dir.mkdirs();
                File out = new File(dir, "picked-" + System.nanoTime() + ".piqua");
                output = new FileOutputStream(out);
                byte[] buffer = new byte[8192];
                int read;
                int total = 0;
                while ((read = input.read(buffer)) != -1) {
                    total += read;
                    if (total > 1024 * 1024)
                        return null;
                    output.write(buffer, 0, read);
                }
                output.flush();
                return out.getAbsolutePath();
            } catch (Exception exception) {
                return null;
            } finally {
                close(output);
                close(input);
            }
        }

        private String writeToUri(Uri uri) {
            InputStream input = null;
            OutputStream output = null;
            try {
                input = new FileInputStream(cachePath);
                output = getActivity().getContentResolver().openOutputStream(uri);
                if (output == null)
                    return null;
                byte[] buffer = new byte[8192];
                int read;
                while ((read = input.read(buffer)) != -1)
                    output.write(buffer, 0, read);
                output.flush();
                return cachePath;
            } catch (Exception exception) {
                return null;
            } finally {
                close(output);
                close(input);
            }
        }

        private void close(java.io.Closeable stream) {
            if (stream == null)
                return;
            try {
                stream.close();
            } catch (Exception exception) {
            }
        }
    }
}
