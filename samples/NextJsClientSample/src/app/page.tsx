import Link from 'next/link';

export default function Home() {
  return (
    <div className="min-h-[calc(100vh-4rem)] bg-gradient-to-br from-blue-50 via-white to-purple-50">
      <div className="max-w-5xl mx-auto px-6 py-16">
        {/* Hero Section */}
        <div className="text-center mb-20">
          <div className="inline-flex items-center gap-2 px-4 py-2 bg-blue-100 text-blue-700 rounded-full text-sm font-medium mb-6">
            <span className="relative flex h-2 w-2">
              <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-blue-400 opacity-75"></span>
              <span className="relative inline-flex rounded-full h-2 w-2 bg-blue-500"></span>
            </span>
            Real-time streaming SDK
          </div>

          <h1 className="text-6xl font-bold mb-6 bg-gradient-to-r from-blue-600 via-purple-600 to-pink-600 bg-clip-text text-transparent">
            OpenRouter.NET
            <br />
            React SDK
          </h1>
          <p className="text-xl text-gray-600 mb-10 max-w-2xl mx-auto leading-relaxed">
            Beautiful streaming chat with artifacts and tool calls.
            <br />
            Perfect order, perfect timing, perfect DX.
          </p>
          <div className="flex gap-4 justify-center flex-wrap">
            <Link
              href="/chat"
              className="group px-8 py-4 bg-blue-500 text-white rounded-xl hover:bg-blue-600 transition-all duration-200 font-semibold shadow-lg shadow-blue-500/30 hover:shadow-xl hover:shadow-blue-500/40 hover:-translate-y-0.5"
            >
              <span className="flex items-center gap-2">
                Try Full Chat
                <span className="group-hover:translate-x-1 transition-transform">→</span>
              </span>
            </Link>
            <Link
              href="/simple"
              className="px-8 py-4 bg-white text-gray-900 rounded-xl hover:bg-gray-50 transition-all duration-200 font-semibold shadow-lg shadow-gray-200 hover:shadow-xl hover:-translate-y-0.5 border border-gray-200"
            >
              Simple Text Example
            </Link>
          </div>
        </div>

        {/* Features */}
        <div className="grid md:grid-cols-2 gap-5 mb-16">
          <FeatureCard
            icon="💬"
            title="Streaming Chat"
            description="Real-time streaming with perfect message history and state management"
            gradient="from-blue-500 to-cyan-500"
          />
          <FeatureCard
            icon="🎨"
            title="Content Blocks"
            description="Text, artifacts, and tool calls interleaved in correct order"
            gradient="from-purple-500 to-pink-500"
          />
          <FeatureCard
            icon="🔧"
            title="Tool Calls"
            description="Track tool execution status with results and errors"
            gradient="from-orange-500 to-red-500"
          />
          <FeatureCard
            icon="📦"
            title="Artifacts"
            description="Real-time artifact generation with streaming support"
            gradient="from-green-500 to-teal-500"
          />
        </div>

        {/* Code Example */}
        <div className="bg-white rounded-2xl shadow-lg shadow-gray-200/50 border border-gray-200 overflow-hidden">
          <div className="px-6 py-4 bg-gradient-to-r from-gray-900 to-gray-800 border-b border-gray-700">
            <div className="flex items-center gap-2">
              <div className="flex gap-1.5">
                <div className="w-3 h-3 rounded-full bg-red-500"></div>
                <div className="w-3 h-3 rounded-full bg-yellow-500"></div>
                <div className="w-3 h-3 rounded-full bg-green-500"></div>
              </div>
              <span className="text-sm text-gray-400 ml-2">Usage Example</span>
            </div>
          </div>
          <div className="p-6 bg-gray-950 overflow-x-auto">
            <pre className="text-sm text-gray-100 font-mono leading-relaxed">
              <code>{`const { state, actions } = useOpenRouterChat({
  endpoints: {
    stream: '/api/stream',
    clearConversation: '/api/conversation',
  },
  defaultModel: 'anthropic/claude-3.5-sonnet',
});

// Send a message
await actions.sendMessage('Create a React component');

// Access messages with interleaved content
state.messages.forEach(message => {
  message.blocks.forEach(block => {
    if (block.type === 'text') {
      console.log(block.content);
    } else if (block.type === 'artifact') {
      console.log(block.title, block.content);
    } else if (block.type === 'tool_call') {
      console.log(block.toolName, block.result);
    }
  });
});`}</code>
            </pre>
          </div>
        </div>
      </div>
    </div>
  );
}

function FeatureCard({
  icon,
  title,
  description,
  gradient,
}: {
  icon: string;
  title: string;
  description: string;
  gradient: string;
}) {
  return (
    <div className="group relative p-6 bg-white rounded-2xl border border-gray-200 hover:border-transparent hover:shadow-xl transition-all duration-300 overflow-hidden">
      {/* Gradient overlay on hover */}
      <div className={`absolute inset-0 bg-gradient-to-br ${gradient} opacity-0 group-hover:opacity-5 transition-opacity duration-300`} />

      <div className="relative">
        <div className="text-4xl mb-4">{icon}</div>
        <h3 className="text-lg font-bold text-gray-900 mb-2">{title}</h3>
        <p className="text-gray-600 leading-relaxed">{description}</p>
      </div>
    </div>
  );
}
