# AuraSync

## Strategic Overview of AuraSync

AuraSync: A Platform for Development and AI Training.

AuraSync is a cutting-edge tool and platform developed by Heimo to optimize developer workflows and provide advanced insights. Designed for both internal use and external teams, AuraSync bridges the gap between human creativity and machine learning, enabling seamless collaboration and innovation.

Key benefits of AuraSync include:

- **Streamlined Workflows**: Enhancing productivity with actionable insights.
- **AI-Driven Insights**: Leveraging data to refine tools and processes.
- **Collaboration Enablement**: Supporting teams with shared knowledge and best practices.

AuraSync is not just a tool; it’s a platform that empowers developers and teams to achieve more, while contributing to the evolution of intelligent systems.

## Objectives of AuraSync (and AuraSync Pulse) Focusing on AI

The objectives are now divided into two interconnected fronts: direct benefits to the developer and strategic benefits for AI training.

### 1. For the Training and Evolution of Heimo's AI:

- **Collect Contextualized Data**: Capture a rich, high-fidelity dataset of human behavior in the Unity Editor (interactions, file types, work categories, tool usage), essential for training Machine Learning models.
- **Identify Human Development Patterns**: Analyze the heartbeats so that AI can learn about best practices, common challenges, and creative approaches of developers.
- **Validate and Refine Assistance AIs**: Utilize activity data to evaluate the effectiveness of code copilots, optimizers, and other AI tools being developed or used by Heimo, ensuring alignment with human workflows.
- **Predict Needs and Automate Tasks**: Develop AI models that can anticipate developer needs (e.g., suggesting code, assets, or workflows) or intelligently automate repetitive tasks based on usage patterns.
- **Assess Training Effectiveness**: Measure the impact of AI training on developer workflows, creating a continuous feedback loop for improving Heimo's AI tools.

### 2. For the Individual Developer (Through AI):

- **Proactive Assistance**: Provide developers with contextualized code suggestions, optimizations, and problem-solving solutions, powered by AIs trained on the team's own data.
- **AI-Powered Workflow Optimization**: Help developers identify and mitigate distractions and bottlenecks, with AI-generated insights into their own productivity patterns.
- **AI-Driven Personalized Learning**: Offer targeted training and resources recommended by AI based on daily activities and challenges, accelerating skill development.
- **Meaningful Gamified Recognition**: Use AI analysis to generate smarter, more rewarding gamification metrics, celebrating complex contributions and real progress.

This approach positions AuraSync as a fundamental pillar in Heimo's AI strategy, enhancing the tool's value and developer engagement.

> ⭐ **Note**: This package is available as a public repository on GitHub to facilitate installation and use.

## Installation

### Via Git Submodules (Recommended for teams)
1. Navigate to your Unity project folder
2. Add the package as a submodule:
   ```bash
   git submodule add https://github.com/heimogamestudio/aurasync-lib.git Packages/com.heimo.aurasync
   git commit -m "Add AuraSync as submodule"
   ```
3. When other team members clone the project, they will need to:
   ```bash
   git clone --recursive <main-project-url>
   ```
   
   Or if they have already cloned:
   ```bash
   git submodule update --init --recursive
   ```

### Via Unity Package Manager (Git URL) 
1. Open your Unity project
2. Go to Window > Package Manager
3. Click the + button in the top-left corner
4. Select "Add package from git URL..."
5. Enter `https://github.com/heimogamestudio/aurasync-lib.git#v1.0.0`
6. Click Add

### Via Local Package
1. Download the package as a .zip file
2. Extract it to your desired location
3. In Unity, go to Window > Package Manager
4. Click the + button in the top-left corner
5. Select "Add package from disk..."
6. Navigate to the extracted package folder and select the package.json file

### Manual Installation (Adding to manifest.json)
Add this line to your `Packages/manifest.json` file:

```json
"com.heimo.aurasync": "https://github.com/heimogamestudio/aurasync-lib.git#v1.0.0"
```

For local installation:

```json
"com.heimo.aurasync": "file:../path/to/com.heimo.aurasync"
```

## Usage

### Basic Setup

1. After installing the package, access the AuraSync Settings from:
   ```
   Tools > Heimo > AuraSync > Settings
   ```

2. Configure your developer details and backend connection:
   - User ID: Your unique identifier
   - Developer Name: Your name or nickname
   - Backend URL: URL of your AuraSync backend
   - API Key: Authorization key for the backend

3. Click "Save" to apply the settings

### Developer Activity Tracking

AuraSync automatically tracks developer activity in Unity and sends the data to your configured backend. This includes:

- Scene edits and saves
- Time spent in Play Mode
- Hierarchy changes
- Project and Git branch information

For more details, see [Developer Activity Tracking](Documentation/DeveloperActivityTracking.md).

### Programmatic Usage

```csharp
// Access the AuraSync manager
AuraSyncManager manager = AuraSyncManager.Instance;

// Re-initialize if needed
manager.Initialize();
```

## License
[Include license information here]

## Support
For questions or support, contact us at contato@heimogames.com.br or visit https://heimogames.com.br
