<H1>How to Add a Unity Package Using the Package Manager and a Git URL</H1>

<h2>Table of Contents</h1>
<ol>
    <li><a href="#how-to-add-a-unity-package-using-the-package-manager-and-a-git-url">Import module to your project</a></li>
    <li><a href="#daily-reward-system">Daily Reward System</a></li>
    <li><a href="#condition-system">Condition System</a></li>
    <li><a href="#mission-system">Mission System</a></li>
    <li><a href="#shop-system">Shop System</a></li>
    <li><a href="#Quest-System-Setup">Quest System</a></li>
    <li><a href="#Time-Marker">Time Marker</a></li>
</ol>

<h2>Import module to your project</h2>
If you want to add a specific Unity package from a GitHub repository, you can do it easily using Unity's Package Manager. Below is a simple guide to help you add a package to your project.

Steps
1. Open the <a href="https://docs.unity3d.com/Manual/upm-ui.html" target="_blank">Unity Package Manager</a><br>
In Unity, go to the top menu and click on Window > Package Manager.
2. Add Package from Git URL
In the Package Manager window, locate the + button in the top left corner.
From the dropdown, select Add package from git URL....

![image](https://github.com/user-attachments/assets/c91d0418-9d95-47df-ba1b-8154a1e5de5f)

4. Insert the Git URL with the Desired Package Path
A text field will appear where you can paste the Git URL of the specific package. For example, to add a package named `[Package-to-use]` from your GitHub repository, use the following URL:

```
https://github.com/NotthingStudioo/FeatureModule.git?path=UnityFeatureModule/Assets/GameModule/[Package-to-use]&branch=H%2B
```

Replace <code>[Package-to-use]</code> with the specific folder name of the package you'd like to include.

4. Confirm and Install
After pasting the URL, press the Enter key or click Add.
Unity will now download and install the package from the specified GitHub repository path into your project.
Example Usage
To use the `Daily Reward` package, for example, you would follow the same steps and insert the following URL:

```
https://github.com/NotthingStudioo/FeatureModule.git?path=UnityFeatureModule/Assets/GameModule/DailyReward&branch=H%2B
```

This will install the Daily Reward system into your project, ready for use!

<h2>Daily Reward System</h2>

<h3>System Architecture</h3>

<img src="https://github.com/user-attachments/assets/4c13b7c8-e194-49f6-81b1-39930255d67e" alt="image" width="400" />

<p><strong>1. Install the <code>DailyReward</code> to the Project Installer</strong></p>

```
https://github.com/NotthingStudioo/FeatureModule.git?path=UnityFeatureModule/Assets/GameModule/DailyReward&branch=H%2B
```

<p>
To integrate the Daily Reward system into your project, you'll need to bind it using the <strong>Zenject</strong> framework (if you're using it) or manually initialize it. Follow these steps:
</p>

<p>
- Open your <code>GameProjectInstaller</code> class (which is a MonoInstaller).<br>
- Add the following code to the <code>InstallBindings</code> method to properly install the Daily Reward system:
</p>

``` Csharp
public class GameProjectInstaller : MonoInstaller
{
    public override void InstallBindings() 
    { 
            DailyRewardInstaller<DailyRewardPresenter>.Install(this.Container); // DailyRewardPresenter is an example for View
    }
}
```

<p><strong>2. Import Blueprint Data</strong></p>
<p>
The <code>DailyReward</code> system relies on blueprint data, typically stored in CSV files, to manage the reward configuration.
</p>
<p>
- <strong>Navigate</strong> to the folder <code>GameModule/DailyReward/Resources/BlueprintDataSample</code>.<br>
- <strong>Move</strong> all the <code>.csv</code> files from this folder to your own project's blueprint folder where other blueprints are stored.<br>
- <strong>Modify the CSV files</strong> to match your reward structure if necessary.
</p>

<p><strong>3. Customize the Daily Reward Slot View</strong></p>
<p>
The <code>DailyRewardSlotView</code> is the visual component that displays the daily rewards to the player. You can either use the default view provided or create your own.
</p>
<p>
- <strong>If you're using the default view</strong>:<br>
   - Move the <code>DailyRewardSlotView</code> to Unity's Addressables system.<br>
   - Optionally, simplify the name of the view.
</p>
<p>
- <strong>If you're creating your own view</strong>:<br>
   - Skip this step, and make sure your custom view adheres to the expected structure required by the Daily Reward system.
</p>

<p><strong>4. Modify the Default Screen Behavior</strong></p>
<p>
By default, the Daily Reward system will open on the <strong>Main Screen</strong> when the game starts. However, you can change this behavior:
</p>
<p>
- <strong>Open the <code>DailyRewardMiscParam</code></strong> file.<br>
- <strong>Find the <code>StartOnScreen</code> parameter</strong> and change its value to the screen name where you'd like the Daily Reward system to appear.
</p>

<p><strong>5. Configure the Reward Loop</strong></p>
<p>
The <code>TimeLoop</code> parameter represents the length of the reward cycle. You can customize it as follows:
</p>
<p>
- <code>7</code> for a 7-day reward cycle.<br>
- <code>30</code> for a 30-day reward cycle.<br>
- <code>365</code> for an annual reward cycle.
</p>

<p>You can now customize the Daily Reward system to fit your game's structure and needs.</p>

<h2>Condition System</h2>
<h3>System architecture</h3>

<figure>
    <img src="https://github.com/user-attachments/assets/e5e83693-7920-4426-98c9-751c53b9cd90" alt="Condition System" width="800" /><br>
    <figcaption>Figure 2: Sequence diagram of the Condition System</figcaption>
</figure>

<p><strong>1. Install the <code>Condition</code> to the Project Installer</strong></p>

```
https://github.com/NotthingStudioo/FeatureModule.git?path=UnityFeatureModule/Assets/GameModule/Condition&branch=H%2B
```

<h2>Mission System</h2>

<p><strong>1. Install the <code>Condition</code> to the Project Installer</strong></p>

```
https://github.com/NotthingStudioo/FeatureModule.git?path=UnityFeatureModule/Assets/GameModule/Condition&branch=H%2B
```

<p><strong>2. Install the <code>Mission</code> to the Project Installer</strong></p>

```
https://github.com/NotthingStudioo/FeatureModule.git?path=UnityFeatureModule/Assets/GameModule/Mission&branch=H%2B
```

<p> The MissionInstaller auto-install the condition system since it need it. Similar to the Daily Reward system, you'll bind it using the <strong>Zenject</strong> framework or manually initialize it: </p>

``` Csharp
public class GameProjectInstaller : MonoInstaller
{
    public override void InstallBindings() 
    {
            MissionInstaller.Install(this.Container);
    }
}
```
<p><strong>3. Import Mission Blueprint Data</strong></p> <p> The <code>Mission</code> system also relies on blueprint data stored in CSV files to manage the mission configuration. </p> <p> - <strong>Navigate</strong> to the folder <code>GameModule/Mission/Resources/BlueprintDataSample</code>.<br> - <strong>Move</strong> all the <code>.csv</code> files from this folder to your project's blueprint folder.<br> - <strong>Modify the CSV files</strong> to match your mission structure if necessary. </p>

<h2>Shop System</h2>
<h3>System Architecture</h3>
<figure>
    <img src="https://github.com/user-attachments/assets/fde6dfcf-5778-449e-9f8a-d9ac2bc868bc" alt="Shop System" width="400" height="600" /><br>
    <figcaption>Figure 3: Flowchart of the Shop System</figcaption>
</figure>


 <p><strong>1. Install the <code>Shop</code> module to the Project Installer</strong></p>
 
```
https://github.com/NotthingStudioo/FeatureModule.git?path=UnityFeatureModule/Assets/GameModule/Shop&branch=H%2B
```

<p>Similar to the Mission System, the <code>Shop</code> system requires installation using the <strong>Zenject</strong> framework. You can integrate it into your game project using the following method:</p>

``` Csharp
public class GameProjectInstaller : MonoInstaller
{
    public override void InstallBindings() 
    {
        TransactionInstaller.Install(this.Container);
    }
}
```

<p>The <code>TransactionInstaller</code> will auto-install dependencies such as the <code>Condition</code> system if they are required by the shop module.</p> <p><strong>2. Configure Shop Data</strong></p> <p>Similar to the Mission system, the <code>Shop</code> system relies on external configuration files such as CSV or JSON for setting up shop items, costs, and rewards. Follow these steps to configure your shop data:</p>
<strong>Navigate</strong> to the folder <code>GameModule/Shop/Resources/ShopDataSample</code>.
<strong>Move</strong> the sample configuration files into your project folder for customization.
<strong>Modify the configuration files</strong> to align with your game's shop structure, prices, and rewards.
By following this setup, the Shop System will be ready to integrate into your game along with other modules like the Mission system.

<h2>Quest System Setup</h2>

<h3>Introduction</h3>
<p>This documentation provides an overview of the Quest System, including details on the MainQuest, SideQuest, and QuestContext CSV files, their structures, and the key properties used in defining quests in your game.</p>

![image](https://github.com/user-attachments/assets/d77a248c-c024-48b9-9e88-f1c226721406)
System Design

<h3>Installation and Setup</h3>
    <strong>Install the Module:</strong>
        
    https://github.com/NotthingStudioo/FeatureModule.git?path=UnityFeatureModule/Assets/GameModule/QuestModule&branch=H%2B
<ol>
    <li><strong>Modify the Blueprint Data:</strong>
        <p>To configure quests, import and modify the following CSV files located in the sample folder to match your specific needs:</p>
        <h4>MainQuest.csv Structure</h4>
        <table border="1">
            <tr><th>Column Name</th><th>Description</th></tr>
            <tr><td>Id</td><td>Unique identifier for the main quest</td></tr>
            <tr><td>QuestType</td><td>Defines the type of quest</td></tr>
            <tr><td>QuestRewardId</td><td>Reference ID of the reward granted upon completion</td></tr>
            <tr><td>QuestRewardType</td><td>Type of reward (e.g., currency, item)</td></tr>
            <tr><td>QuestRewardValue</td><td>Quantity or value of the reward</td></tr>
            <tr><td>QuestIcon</td><td>Icon representing the quest visually</td></tr>
            <tr><td>QuestDescription</td><td>Brief description of the quest</td></tr>
            <tr><td>TaskId</td><td>Identifier for the task associated with the quest</td></tr>
            <tr><td>TrackingType</td><td>Specifies tracking method for quest progress</td></tr>
            <tr><td>TaskState</td><td>Current state of the task (e.g., in progress, completed)</td></tr>
            <tr><td>QuestContextIds</td><td>Associated context IDs for quest conditions</td></tr>
            <tr><td>Description</td><td>Detailed quest instructions or objectives</td></tr>
            <tr><td>TaskName</td><td>Name of the quest task</td></tr>
            <tr><td>RequirementType</td><td>Specifies the requirement type (e.g., level, item)</td></tr>
            <tr><td>RequirementId</td><td>ID of the requirement item or condition</td></tr>
            <tr><td>RequirementValue</td><td>Quantity or value required for completion</td></tr>
            <tr><td>TaskRewardId</td><td>Reference ID of the task reward</td></tr>
            <tr><td>TaskRewardType</td><td>Type of reward for completing the task</td></tr>
            <tr><td>TaskRewardValue</td><td>Quantity or value of the task reward</td></tr>
        </table>
        <h4>SideQuest.csv Structure</h4>
        <table border="1">
            <tr><th>Column Name</th><th>Description</th></tr>
            <tr><td>Id</td><td>Unique identifier for the side quest</td></tr>
            <tr><td>QuestType</td><td>Defines the type of side quest</td></tr>
            <tr><td>QuestRewardId</td><td>Reference ID of the reward granted upon completion</td></tr>
            <tr><td>QuestRewardType</td><td>Type of reward for the side quest</td></tr>
            <tr><td>QuestRewardValue</td><td>Quantity or value of the side quest reward</td></tr>
            <tr><td>QuestIcon</td><td>Icon representing the side quest visually</td></tr>
            <tr><td>QuestDescription</td><td>Description of the side quest objectives</td></tr>
            <tr><td>TaskId</td><td>Identifier for the task related to the side quest</td></tr>
            <tr><td>TrackingType</td><td>Specifies tracking method for side quest progress</td></tr>
            <tr><td>TaskState</td><td>Current state of the task (e.g., in progress, completed)</td></tr>
            <tr><td>QuestContextIds</td><td>Associated context IDs for quest conditions</td></tr>
            <tr><td>Description</td><td>Detailed side quest instructions or objectives</td></tr>
            <tr><td>TaskName</td><td>Name of the side quest task</td></tr>
            <tr><td>RequirementType</td><td>Specifies the requirement type (e.g., level, item)</td></tr>
            <tr><td>RequirementId</td><td>ID of the requirement item or condition</td></tr>
            <tr><td>RequirementValue</td><td>Quantity or value required for completion</td></tr>
            <tr><td>TaskRewardId</td><td>Reference ID of the task reward</td></tr>
            <tr><td>TaskRewardType</td><td>Type of reward for completing the task</td></tr>
            <tr><td>TaskRewardValue</td><td>Quantity or value of the task reward</td></tr>
        </table>
        <h4>QuestContext.csv Structure</h4>
        <table border="1">
            <tr><th>Column Name</th><th>Description</th></tr>
            <tr><td>Id</td><td>Unique identifier for the quest context</td></tr>
            <tr><td>QuestContextType</td><td>Type of context (e.g., location, NPC)</td></tr>
            <tr><td>Data</td><td>Additional data specific to the quest context type</td></tr>
        </table>
    </li>
    <li><strong>Inject the QuestProviderService:</strong> To manage quests, inject the QuestProviderService. This service provides two main functions:
        <ul>
            <li><strong>GiveQuestToUser:</strong> Initializes and assigns a quest to the user.</li>
            <li><strong>StartQuest:</strong> Sets the quest’s status to in-progress.</li>
        </ul>
    </li>
    <li><strong>Track Quest Progress:</strong> Use the <code>TrackingQuestSignal</code> to update quest progress and manage quest completion status based on in-game events.</li>
</ol>

``` Csharp
        public class TrackingQuestSignal
        {
            public string RequirementType  { get; }
            public string RequirementId    { get; }
            public int    RequirementValue { get; }
        }
```

<h2>Time Marker</h2>

<h3>Introduction</h3>
<p>The TimeMarker module allows other classes to manage and interact with time markers through the <code>TimeMarkService</code> class. This service can handle operations such as adding, updating, and removing time marks, as well as checking for date-based differences. The TimeMarker also supports real-time tracking of time spans with <code>ReactiveProperty&lt;float&gt;</code> to provide live updates.</p>

<h3>Installation</h3>
<ol>
    <li><strong>Install the Module:</strong> Follow your standard steps to add the TimeMarker module to your project.</li>
    
```csharp
https://github.com/NotthingStudioo/FeatureModule.git?path=UnityFeatureModule/Assets/GameModule/TimeMarker&branch=H%2B
```

<li><strong>Register the TimeMarkService:</strong> Add the following code to your <code>GameProjectInstaller</code> or equivalent MonoInstaller to bind <code>TimeMarkService</code> to the Zenject container:</li>
</ol>

```csharp
public class GameProjectInstaller : MonoInstaller
{
    public override void InstallBindings() 
    {
        TimeMarkInstaller.Install(this.Container);
    }
}
```
<h3>Function Documentation</h3> <h4><code>AddTimeMark</code></h4> <p>Adds a new time mark with the specified key and <code>DateTime</code>.</p> <ul> <li><strong>Input:</strong> <ul> <li><code>string key</code>: Unique identifier for the time mark.</li> <li><code>DateTime time</code>: The time to be stored for the key.</li> </ul> </li> <li><strong>Output:</strong> None</li> </ul> <h4><code>GetOrCreateTimeMark</code></h4> <p>Retrieves an existing time mark or creates a new one if it doesn't exist.</p> <ul> <li><strong>Input:</strong> <ul> <li><code>string key</code>: Unique identifier for the time mark.</li> <li><code>out DateTime time</code>: Outputs the retrieved or created time.</li> </ul> </li> <li><strong>Output:</strong> <code>bool</code> indicating if the time mark was retrieved (<code>true</code>) or created (<code>false</code>).</li> </ul> <h4><code>RemoveTimeMark</code></h4> <p>Removes a time mark from both the data controller and the cached dictionary.</p> <ul> <li><strong>Input:</strong> <code>string key</code>: Unique identifier for the time mark to remove.</li> <li><strong>Output:</strong> None</li> </ul> <h4><code>UpdateTimeMark</code></h4> <p>Updates an existing time mark to a new <code>DateTime</code> and recalculates any cached time span values.</p> <ul> <li><strong>Input:</strong> <ul> <li><code>string key</code>: Unique identifier for the time mark.</li> <li><code>DateTime time</code>: New time to update for the key.</li> </ul> </li> <li><strong>Output:</strong> None</li> </ul> <h4><code>IsNewDay</code></h4> <p>Checks if the stored date for a time mark is a day before the current date.</p> <ul> <li><strong>Input:</strong> <code>string timeMarkKey</code>: Unique identifier for the time mark.</li> <li><strong>Output:</strong> <code>bool</code> indicating if the date is before today (<code>true</code>) or not (<code>false</code>).</li> </ul> <h4><code>GetDayDifference</code></h4> <p>Calculates the difference in days between the stored date for a time mark and the current date.</p> <ul> <li><strong>Input:</strong> <code>string timeMarkKey</code>: Unique identifier for the time mark.</li> <li><strong>Output:</strong> <code>int</code> representing the number of days difference.</li> </ul> <h4><code>ResetTimeMark</code></h4> <p>Removes a time mark from both the data controller and the cached dictionary, resetting it.</p> <ul> <li><strong>Input:</strong> <code>string timeMarkKey</code>: Unique identifier for the time mark to reset.</li> <li><strong>Output:</strong> None</li> </ul> <h4><code>GetOrCreateTimeSpan</code></h4> <p>Retrieves a <code>ReactiveProperty&lt;float&gt;</code> representing the time span in seconds for the specified key. Creates and initializes the property if it doesn’t exist.</p> <ul> <li><strong>Input:</strong> <code>string key</code>: Unique identifier for the time span.</li> <li><strong>Output:</strong> <code>UniTask&lt;ReactiveProperty&lt;float&gt;&gt;</code> representing the time span in seconds since the stored time for the key.</li> </ul>
