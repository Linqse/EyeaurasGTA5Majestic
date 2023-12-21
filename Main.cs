using System.Collections.Immutable;
using System.IO;
using System.Reactive.Disposables;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.Structure;
using EyeAuras.OpenCVAuras.ML.Yolo;
using EyeAuras.Roxy.Services;
using EyeAuras.Roxy.Shared;
using EyeAuras.Roxy.Shared.Actions.SendInput;
using EyeAuras.Web.Repl.Component.Image;
using PoeShared.Modularity;
using PoeShared.Native;
using PoeShared.UI;

namespace EyeAuras.Web.Repl.Component;

public partial class Main : WebUIComponent {
   
    
    public Main(){
        
    }
    [Dependency] public ISendInputController SendInputController { get; init; }
    
    [Dependency] public IHotkeyConverter HotkeyConverter { get; init; }
    [Dependency] public IAppArguments appArguments { get; init; }


    private IWinExistsTrigger WinExistsTrigger =>
        AuraTree.FindAuraByPath(@".\WinExists").Triggers.Items.OfType<IWinExistsTrigger>().First();

    private static readonly SendInputArgs DefaultSendInputArgs = new()
    {
        
        MinDelay = TimeSpan.FromMilliseconds(25),
        MaxDelay = TimeSpan.FromMilliseconds(35),
        InputSimulatorId = "Windows Input",
        InputEventType = InputEventType.KeyPress
    };
    
    private static readonly SendInputArgs DefaultSendInputArgsMouse = new()
    {
        InputSimulatorId = "Windows Input",
        InputEventType = InputEventType.KeyPress
    };

    private IImageSearchTrigger TriggerE =>
        AuraTree.FindAuraByPath(@".\Images\E").Triggers.Items.OfType<IImageSearchTrigger>().First(); 
    
    private IMLSearchTrigger Gather =>
        AuraTree.FindAuraByPath(@".\ML\Gather").Triggers.Items.OfType<IMLSearchTrigger>().First();

    private IMLSearchTrigger FishMl =>
        AuraTree.FindAuraByPath(@".\ML\Fish").Triggers.Items.OfType<IMLSearchTrigger>().First();

    private IMLSearchTrigger CapchaML =>
        AuraTree.FindAuraByPath(@".\ML\Capcha").Triggers.Items.OfType<IMLSearchTrigger>().First();

    private IImageSearchTrigger TriggerRange => AuraTree.FindAuraByPath(@".\Images\Range").Triggers.Items
        .OfType<IImageSearchTrigger>().First();

    private IHotkeyIsActiveTrigger Hotkey =>
        AuraTree.FindAuraByPath(@".\Key").Triggers.Items.OfType<IHotkeyIsActiveTrigger>().First();

    private IWebUIAuraOverlay Overlay => AuraTree.Aura.Overlays.Items.OfType<IWebUIAuraOverlay>().First();
    
    
    private bool Orange { get; set; } 
    private bool Stones { get; set; }
    private bool Tree { get; set; } 
    private bool Fish { get; set; } 
    private bool Capcha { get; set; }

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    private async Task Close()
    {
        try
        {
            await SendKey("`");
            Hotkey.TriggerValue = false;
            SetForegroundWindow(WinExistsTrigger.ActiveWindow.Handle);
        }
        catch
        {
            Log.Info("Problem with activations");
        }
    }

    


    private void ToggleLock()
    {
        
        Overlay.IsLocked = !Overlay.IsLocked;
    }


    /*private async Task CheckDownloadML()
    {
        var path = Path.Combine(appArguments.AppDataDirectory, "EyeSquad");
        var checker = new AI.Check();
        try
        {
            var result = await checker.CheckOrDownloadAsync(path);

            if (result.success)
            {
                var newCaptchaFileInfo = new FileInfo(result.capcha);
                if (CapchaML.MLModelPath?.FullName != newCaptchaFileInfo.FullName)
                {
                    CapchaML.MLModelPath = newCaptchaFileInfo;
                }

                var newFishFileInfo = new FileInfo(result.fish);
                if (FishMl.MLModelPath?.FullName != newFishFileInfo.FullName)
                {
                    FishMl.MLModelPath = newFishFileInfo;
                }

                var newGatherFileInfo = new FileInfo(result.orangestones);
                if (Gather.MLModelPath?.FullName != newGatherFileInfo.FullName)
                {
                    Gather.MLModelPath = newGatherFileInfo;
                } 
            }
        }
        catch
        {
            Log.Info("Download \\ set path GTA5ML error"); 
        } 
    }*/


    protected override async Task HandleAfterFirstRender()
    {
        Disposable.Create(() => Log.Info("Disposed")).AddTo(Anchors);
        
        var key = TriggerE.WhenAnyValue(x => x.IsActive)
            .Where(x => x.HasValue && x.Value)
            .Subscribe(_ => Gathering())
            .AddTo(Anchors);
        this.Hotkey.WhenAnyValue(x => x.IsActive)
            .Where(x => x.HasValue && x.Value)
            .Subscribe(async _ => await SendKey("`"))
            .AddTo(Anchors);
        
        CapchaML.ResultStream.Subscribe(result => 
        {
            try
            {
                if (Capcha)
                {
                    CaptchaPress(result.Predictions);
                }
            }
            catch
            {
                Log.Info("Problem with ML");
                
            }
        }).AddTo(Anchors);

        Gather.ImageSink.Subscribe(x => Task.Run(() => SaveScreen(x))).AddTo(Anchors);

        //CheckDownloadML(); 

    }


    private async Task SaveScreen(Image<Bgr, byte> img)
    {
        try
        {
            string fileName = Path.GetRandomFileName() + ".png";
            string path = Path.Combine(@"E:\GatherScreens", fileName);
            Directory.CreateDirectory(@"E:\GatherScreens");
            img.Save(path);
        }
        catch (Exception ex)
        {
            Log.Error("Ошибка при сохранении изображения: " + ex.Message);
        }
    }
    
    private async Task CaptchaPress(ImmutableArray<YoloPrediction> predictions)
    {
        var key = predictions.FirstOrDefault().Label.Name;
        if(key != null) SendKey($"{key}");

    }
    
    
    private async Task Gathering()
    {
        
        if (Orange)
        {
            await SendKey("E");
            await Task.Delay(200);
            await PickUpOranges();
        }

        if (Stones)
        {
            await SendKey("E");
            await Task.Delay(200);
            await PickUpStones();
        }

        if (Tree)
        {
            await SendKey("E");
            await Task.Delay(200);
            await PickUpTree();
        }

        if (Capcha)
        {
            await SendKey("E");
            await Task.Delay(200);
        }
    }
    
    
    private async Task FishingTime()
    {
        await SendKey("I");
        await Task.Delay(1500);
        await SendKey("I");
        await SendKey("E");

        while (Fish)
        {
            Log.Info("Waiting fish");
            await CatchFish();
            if (!Fish) break;

            Log.Info("Waiting range");
            await PressRange();
            if (!Fish) break;

            Log.Info("Delay 2500");
            await Task.Delay(2500);
            if (!Fish) break;

            await SendKey("E");
            Log.Info("Start Fishing");
        }
    }

    private async Task PressRange()
    {
        try
        {
            var watch = new Stopwatch();
            watch.Start();
            await SendKey("MouseLeft", null, "KeyDown");
            while (TriggerRange.IsActive == true && Fish)
            {
                await GetImage.RefreshAndGet(TriggerRange);
                await Task.Delay(200);
                if (watch.ElapsedMilliseconds > 20000)
                {
                    await SendKey("E");
                }
            }
        }
        catch
        {
            Log.Info("Error PressRange");
        }
        finally
        {
            await SendKey("MouseLeft", null, "KeyUp");
        }
    }

    private async Task CatchFish()
    {
        string lastKeyPressed = string.Empty;
        bool isFirstDetection = true;

        try
        {
            RectangleF saveLocation = new RectangleF();

            while (TriggerRange.IsActive != true && Fish)
            {
                var check = await GetImage.RefreshAndGetML(FishMl);

                if (check.IsActive == true)
                {
                    var prediction = check.Predictions.FirstOrDefault();
                    if (prediction != null) 
                    {
                        var newLocation = prediction.Rectangle;
                        if (newLocation.X > saveLocation.X && lastKeyPressed != "A")
                        {
                            if (lastKeyPressed == "D")
                            {
                                await SendKey("D", null, "KeyUp");
                            }

                            await SendKey("A", null, "KeyDown");
                            lastKeyPressed = "A";
                        }
                        else if (newLocation.X < saveLocation.X && lastKeyPressed != "D")
                        {
                            if (lastKeyPressed == "A")
                            {
                                await SendKey("A", null, "KeyUp");
                            }

                            await SendKey("D", null, "KeyDown");
                            lastKeyPressed = "D";
                        }

                        saveLocation = newLocation;
                        await Task.Delay(200); 
                    }
                }
                else
                {
                    if (isFirstDetection)
                    {
                        isFirstDetection = false;
                        await Task.Delay(1000); 
                    }
                    else
                    {
                        await Task.Delay(200); 
                        await GetImage.RefreshAndGet(TriggerRange);
                    }
                }
            }
        }
        catch
        {
            Log.Info("Fishing error");
        }
        finally
        {
            if (!string.IsNullOrEmpty(lastKeyPressed))
            {
                await SendKey(lastKeyPressed, null, "KeyUp");
            }
        }
    }


    
    
    
    private async Task PickUpTree()
    {
        var result = await GetImage.RefreshAndGet(TriggerRange);
        while (result.IsActive == true)
        {
            await SendKey("MouseLeft");
            result = await GetImage.RefreshAndGet(TriggerRange);
            await Task.Delay(200); 
        }

        await HandleTreePredictions();
        await Task.Delay(200);
        await HandleTreePredictions();
    }

    private async Task HandleTreePredictions()
    {
        var tree = await GetImage.RefreshAndGetML(Gather);
        if (tree.IsActive == true && Tree)
        {
            var window = Gather.ActiveWindow.WindowBounds;
            int predictionCount = tree.Predictions.Count();

            for (int i = 0; i < predictionCount; i++)
            {
                var prediction = tree.Predictions[i];
                var centerPoint = ConvertToOriginalCoordinates(prediction.Rectangle, window);
                await SendKey("MouseLeft", centerPoint);

                
                int delay = (i == predictionCount - 1) ? 200 : 150;
                await Task.Delay(delay);
            }
        }
    }
    

    private async Task PickUpStones()
    {
        var result = await GetImage.RefreshAndGet(TriggerRange);
        if (result.IsActive == true)
        {
            while (result.IsActive == true)
            {
                await SendKey("MouseLeft");
                await Task.Delay(200);
                result = await GetImage.RefreshAndGet(TriggerRange);
            }
        }
        else
        {
            return;
        }

        await Task.Delay(200);
        var stones = await GetImage.RefreshAndGetML(Gather);
        if (stones.IsActive == true && Stones)
        {
            var window = Gather.ActiveWindow.WindowBounds;
            int predictionCount = stones.Predictions.Count();

            for (int i = 0; i < predictionCount; i++)
            {
                var prediction = stones.Predictions[i];
                var centerPoint = ConvertToOriginalCoordinates(prediction.Rectangle, window);
                await SendKey("MouseLeft", centerPoint);
                
                if (i == predictionCount - 2)
                {
                    await Task.Delay(200);
                }
                else
                {
                    await Task.Delay(150);
                }
            }
        }
        
    }
    
    private async Task PickUpOranges()
    {
        var oranges = await GetImage.RefreshAndGetML(Gather);
        if (oranges.IsActive == true)
        {
            var window = Gather.ActiveWindow.WindowBounds;

            foreach (var prediction in oranges.Predictions)
            {
                var centerPoint = ConvertToOriginalCoordinates(prediction.Rectangle, window);
                await SendKey("MouseLeft", centerPoint);
            }
        }
    }

    private Point ConvertToOriginalCoordinates(RectangleF predictionRectangle, Rectangle windowBounds)
    {
        const int letterBoxedImageSize = 640;

        
        double scaleX = (double)windowBounds.Width / letterBoxedImageSize;
        double scaleY = (double)windowBounds.Height / letterBoxedImageSize;

        
        double letterBoxingX = 0;
        double letterBoxingY = 0;
        if (windowBounds.Width > windowBounds.Height)
        {
            double aspectRatio = (double)windowBounds.Width / windowBounds.Height;
            double imageAspectRatio = 1.0; 
            if (aspectRatio > imageAspectRatio)
            {
                
                scaleY = scaleX;
                letterBoxingY = (letterBoxedImageSize - (windowBounds.Height / scaleY)) / 2;
            }
            else
            {
                
                scaleX = scaleY;
                letterBoxingX = (letterBoxedImageSize - (windowBounds.Width / scaleX)) / 2;
            }
        }

        
        var originalX = (predictionRectangle.X - letterBoxingX) * scaleX;
        var originalY = (predictionRectangle.Y - letterBoxingY) * scaleY;
        var originalWidth = predictionRectangle.Width * scaleX;
        var originalHeight = predictionRectangle.Height * scaleY;

        
        var centerX = originalX + (originalWidth / 2);
        var centerY = originalY + (originalHeight / 2);

        
        return new Point((int)Math.Round(centerX), (int)Math.Round(centerY));
    }


    
    private async Task SendKey(string key, Point? point = null, string inputEventType = null)
    {
        var activeWindow = WinExistsTrigger.ActiveWindow;
        if (activeWindow == null)
        {
            Log.Info("Window is null, break Send Input");
            return;
        }

        var hotkey = HotkeyConverter.ConvertFromString(key);

        InputEventType eventType = InputEventType.KeyPress; // Default value
        if (inputEventType == "KeyDown")
        {
            eventType = InputEventType.KeyDown;
        }
        else if (inputEventType == "KeyUp")
        {
            eventType = InputEventType.KeyUp;
        }

        if (point.HasValue)
        {
            await SendInputController.Send(DefaultSendInputArgsMouse with
            {
                MouseLocation = point.Value,
                Window = activeWindow,
                Gesture = hotkey,
                InputEventType = eventType 
            }, CancellationToken.None);
        }
        else 
        {
            await SendInputController.Send(DefaultSendInputArgs with
            {
                Window = activeWindow,
                Gesture = hotkey,
                InputEventType = eventType 
            }, CancellationToken.None);
        }
    }

    
}