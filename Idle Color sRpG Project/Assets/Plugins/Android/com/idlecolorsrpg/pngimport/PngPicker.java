package com.idlecolorsrpg.pngimport;

import android.app.Activity;
import android.app.Fragment;
import android.content.Intent;
import android.net.Uri;
import android.os.Handler;
import android.os.Looper;
import com.unity3d.player.UnityPlayer;
import java.io.File;
import java.io.FileOutputStream;
import java.io.InputStream;

public class PngPicker {
    public static void open(String receiverName) {
        try {
            Activity activity = UnityPlayer.currentActivity;
            PngPickerFragment fragment = (PngPickerFragment) activity.getFragmentManager().findFragmentByTag("PngPickerFragment");
            if (fragment == null) {
                fragment = new PngPickerFragment();
                activity.getFragmentManager().beginTransaction().add(fragment, "PngPickerFragment").commitAllowingStateLoss();
                activity.getFragmentManager().executePendingTransactions();
            }
            fragment.open(receiverName);
        } catch (Exception exception) {
            UnityPlayer.UnitySendMessage(receiverName, "OnPickedPath", "");
        }
    }

    public static class PngPickerFragment extends Fragment {
        private String receiverName = "PngFilePickerReceiver";
        private boolean pendingOpen;
        private long launchAt;
        private int instantCancelRetries;
        private boolean legacyPicker;
        private static final int REQUEST = 61008;

        public void open(String name) {
            receiverName = name;
            pendingOpen = true;
            instantCancelRetries = 0;
            legacyPicker = false;
            if (isResumed())
                scheduleLaunch();
        }

        @Override
        public void onResume() {
            super.onResume();
            if (pendingOpen)
                scheduleLaunch();
        }

        // Adding the fragment and opening the picker in the same turn makes Android
        // return cancel immediately. Wait, and if that still happens, open once more.
        private void scheduleLaunch() {
            pendingOpen = false;
            new Handler(Looper.getMainLooper()).postDelayed(new Runnable() {
                @Override
                public void run() {
                    if (!isAdded()) {
                        pendingOpen = true;
                        return;
                    }
                    launchAt = System.currentTimeMillis();
                    try {
                        String action = legacyPicker ? Intent.ACTION_GET_CONTENT : Intent.ACTION_OPEN_DOCUMENT;
                        startActivityForResult(openIntent(action), REQUEST);
                    } catch (Exception exception) {
                        try {
                            startActivityForResult(openIntent(Intent.ACTION_GET_CONTENT), REQUEST);
                        } catch (Exception again) {
                            UnityPlayer.UnitySendMessage(receiverName, "OnPickedPath", "");
                        }
                    }
                }
            }, 400);
        }

        private Intent openIntent(String action) {
            Intent intent = new Intent(action);
            intent.setType("image/png");
            intent.addCategory(Intent.CATEGORY_OPENABLE);
            intent.addFlags(Intent.FLAG_GRANT_READ_URI_PERMISSION);
            return intent;
        }

        @Override
        public void onActivityResult(int requestCode, int resultCode, Intent data) {
            super.onActivityResult(requestCode, resultCode, data);
            if (requestCode != REQUEST)
                return;
            if (resultCode != Activity.RESULT_OK || data == null || data.getData() == null) {
                long elapsed = System.currentTimeMillis() - launchAt;
                if (instantCancelRetries < 1 && elapsed >= 0 && elapsed < 800) {
                    instantCancelRetries++;
                    legacyPicker = true;
                    pendingOpen = true;
                    scheduleLaunch();
                    return;
                }
                instantCancelRetries = 0;
            legacyPicker = false;
                UnityPlayer.UnitySendMessage(receiverName, "OnPickedPath", "");
                return;
            }
            instantCancelRetries = 0;
            legacyPicker = false;
            String path = copyToCache(data.getData());
            UnityPlayer.UnitySendMessage(receiverName, "OnPickedPath", path == null ? "" : path);
        }

        private String copyToCache(Uri uri) {
            InputStream input = null;
            FileOutputStream output = null;
            try {
                input = getActivity().getContentResolver().openInputStream(uri);
                if (input == null)
                    return null;
                File cache = getActivity().getCacheDir();
                File[] oldFiles = cache.listFiles();
                if (oldFiles != null) {
                    for (int i = 0; i < oldFiles.length; i++) {
                        String name = oldFiles[i].getName();
                        if (name.startsWith("picked") && name.endsWith(".png"))
                            oldFiles[i].delete();
                    }
                }
                File out = new File(cache, "picked-" + System.nanoTime() + ".png");
                output = new FileOutputStream(out);
                byte[] buffer = new byte[8192];
                int read;
                while ((read = input.read(buffer)) != -1)
                    output.write(buffer, 0, read);
                output.flush();
                output.getFD().sync();
                return out.getAbsolutePath();
            } catch (Exception exception) {
                return null;
            } finally {
                try {
                    if (output != null)
                        output.close();
                } catch (Exception exception) {
                }
                try {
                    if (input != null)
                        input.close();
                } catch (Exception exception) {
                }
            }
        }
    }
}
