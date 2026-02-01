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
      
      return webpackConfig;
    },
  },
};

