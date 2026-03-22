const fs = require('fs');
const os = require('os');
const path = require('path');

const getCacheRoot = () => {
  const envRoot = process.env.CACHE_ROOT;
  if (envRoot) return envRoot;
  if (fs.existsSync('D:\\')) return 'D:\\Temp';
  return os.tmpdir();
};

const cacheRoot = getCacheRoot();

const ensureDir = (dirPath) => {
  try {
    fs.mkdirSync(dirPath, { recursive: true });
  } catch (e) {
  }
};

module.exports = {
  webpack: {
    configure: (webpackConfig) => {
      // Exclude node_modules from source-map-loader to suppress warnings
      const sourceMapLoaderRule = webpackConfig.module.rules.find(
        (rule) => 
          rule.enforce === 'pre' && 
          rule.use && 
          Array.isArray(rule.use) &&
          rule.use.some((u) => 
            (typeof u === 'string' && u.includes('source-map-loader')) ||
            (typeof u === 'object' && u.loader && u.loader.includes('source-map-loader'))
          )
      );
      
      if (sourceMapLoaderRule) {
        // Exclude node_modules from source map processing
        sourceMapLoaderRule.exclude = /node_modules/;
      }

      if (webpackConfig.mode === 'production') {
        webpackConfig.devtool = false;
      }

      const webpackCacheDir = path.join(cacheRoot, 'webpack-cache');
      ensureDir(webpackCacheDir);
      webpackConfig.cache = { type: 'filesystem', cacheDirectory: webpackCacheDir };

      const eslintCacheDir = path.join(cacheRoot, 'eslint-cache');
      ensureDir(eslintCacheDir);
      if (Array.isArray(webpackConfig.plugins)) {
        webpackConfig.plugins.forEach((plugin) => {
          if (
            plugin &&
            plugin.constructor &&
            plugin.constructor.name === 'ESLintWebpackPlugin' &&
            plugin.options
          ) {
            plugin.options.cache = true;
            plugin.options.cacheLocation = path.join(eslintCacheDir, '.eslintcache');
          }
        });
      }

      const babelCacheDir = path.join(cacheRoot, 'babel-cache');
      ensureDir(babelCacheDir);
      const updateBabelLoader = (rules) => {
        if (!Array.isArray(rules)) return;
        rules.forEach((rule) => {
          if (rule.oneOf) updateBabelLoader(rule.oneOf);
          if (rule.rules) updateBabelLoader(rule.rules);

          const candidates = [];
          if (rule.use) candidates.push(rule.use);
          if (rule.loader) candidates.push({ loader: rule.loader, options: rule.options });

          candidates.forEach((candidate) => {
            const uses = Array.isArray(candidate) ? candidate : [candidate];
            uses.forEach((u) => {
              if (!u || typeof u !== 'object') return;
              if (typeof u.loader === 'string' && u.loader.includes('babel-loader')) {
                if (!u.options) u.options = {};
                u.options.cacheDirectory = babelCacheDir;
                u.options.cacheCompression = false;
              }
            });
          });
        });
      };
      updateBabelLoader(webpackConfig.module?.rules);

      if (webpackConfig.optimization && Array.isArray(webpackConfig.optimization.minimizer)) {
        webpackConfig.optimization.minimizer.forEach((minimizer) => {
          if (minimizer && minimizer.constructor && minimizer.constructor.name === 'TerserPlugin') {
            minimizer.options = minimizer.options || {};
            minimizer.options.parallel = false;
          }
          if (minimizer && minimizer.constructor && minimizer.constructor.name === 'CssMinimizerPlugin') {
            minimizer.options = minimizer.options || {};
            minimizer.options.parallel = false;
          }
        });
      }
      
      return webpackConfig;
    },
  },
};
