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
        private static final int REQUEST = 61008;

        public void open(String name) {
            receiverName = name;
            pendingOpen = true;
            if (isResumed())
                scheduleLaunch();
        }

        @Override
        public void onResume() {
            super.onResume();
            if (pendingOpen)
                scheduleLaunch();
        }

        // 断片を足した直後に startActivityForResult すると、Android は選択画面を出さず
        // すぐにキャンセルとして返す。1回目のタップが「ファイルを選びませんでした」になる。
        // 再開後、次の周回で開く。
        private void scheduleLaunch() {
            pendingOpen = false;
            new Handler(Looper.getMainLooper()).post(new Runnable() {
                @Override
                public void run() {
                    if (!isAdded()) {
                        pendingOpen = true;
                        return;
                    }
                    Intent intent = new Intent(Intent.ACTION_GET_CONTENT);
                    intent.setType("image/png");
                    intent.addCategory(Intent.CATEGORY_OPENABLE);
                    try {
                        startActivityForResult(intent, REQUEST);
                    } catch (Exception exception) {
                        UnityPlayer.UnitySendMessage(receiverName, "OnPickedPath", "");
                    }
                }
            });
        }

        @Override
        public void onActivityResult(int requestCode, int resultCode, Intent data) {
            super.onActivityResult(requestCode, resultCode, data);
            if (requestCode != REQUEST)
                return;
            if (resultCode != Activity.RESULT_OK || data == null || data.getData() == null) {
                UnityPlayer.UnitySendMessage(receiverName, "OnPickedPath", "");
                return;
            }
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