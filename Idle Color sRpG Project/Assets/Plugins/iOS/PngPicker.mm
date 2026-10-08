#import <UIKit/UIKit.h>

extern void UnitySendMessage(const char *objectName, const char *methodName, const char *message);
extern UIViewController *UnityGetGLViewController();

@interface PngPickerDelegate : NSObject<UIDocumentPickerDelegate>
@property (nonatomic, copy) NSString *receiver;
@end

@implementation PngPickerDelegate

- (void)documentPicker:(UIDocumentPickerViewController *)controller didPickDocumentsAtURLs:(NSArray<NSURL *> *)urls
{
    if (urls.count == 0)
    {
        [self sendPath:@""];
        return;
    }
    [self copyAndSend:urls[0]];
}

- (void)documentPicker:(UIDocumentPickerViewController *)controller didPickDocumentAtURL:(NSURL *)url
{
    [self copyAndSend:url];
}

- (void)documentPickerWasCancelled:(UIDocumentPickerViewController *)controller
{
    [self sendPath:@""];
}

- (void)copyAndSend:(NSURL *)url
{
    if (url == nil)
    {
        [self sendPath:@""];
        return;
    }
    BOOL scoped = [url startAccessingSecurityScopedResource];
    NSString *dest = [NSTemporaryDirectory() stringByAppendingPathComponent:@"picked.png"];
    [[NSFileManager defaultManager] removeItemAtPath:dest error:nil];
    NSError *error = nil;
    BOOL copied = [[NSFileManager defaultManager] copyItemAtURL:url toURL:[NSURL fileURLWithPath:dest] error:&error];
    if (scoped)
        [url stopAccessingSecurityScopedResource];
    if (!copied)
    {
        [self sendPath:@""];
        return;
    }
    [self sendPath:dest];
}

- (void)sendPath:(NSString *)path
{
    const char *receiver = self.receiver != nil ? [self.receiver UTF8String] : "PngFilePickerReceiver";
    const char *message = path != nil ? [path UTF8String] : "";
    UnitySendMessage(receiver, "OnPickedPath", message);
}

@end

static PngPickerDelegate *pngPickerDelegate = nil;

extern "C" void PngPicker_Open(const char *receiver)
{
    if (pngPickerDelegate == nil)
        pngPickerDelegate = [PngPickerDelegate new];
    pngPickerDelegate.receiver = receiver != NULL ? [NSString stringWithUTF8String:receiver] : @"PngFilePickerReceiver";

    dispatch_async(dispatch_get_main_queue(), ^{
        UIDocumentPickerViewController *picker = [[UIDocumentPickerViewController alloc] initWithDocumentTypes:@[@"public.png"] inMode:UIDocumentPickerModeImport];
        picker.delegate = pngPickerDelegate;
        picker.modalPresentationStyle = UIModalPresentationFormSheet;
        UIViewController *root = UnityGetGLViewController();
        [root presentViewController:picker animated:YES completion:nil];
    });
}