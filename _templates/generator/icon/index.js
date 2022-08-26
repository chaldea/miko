const fs = require('fs');
const path = require('path');

module.exports = {
  params: ({ args }) => {
    const data = { ...args };
    data.context = JSON.parse(Buffer.from(args.json, 'base64').toString());
    const svgRoot = data.context.svgRoot;
    const svgs = fs.readdirSync(svgRoot);
    const icons = [];
    for (let svg of svgs) {
      const svgContent = fs
        .readFileSync(path.resolve(svgRoot, svg), 'utf8')
        .replaceAll('"', '""');
      icons.push({
        name: svg.replace('.svg', ''),
        content: svgContent,
      });
    }
    data.context.icons = icons;
    delete data.json;
    return data;
  }
}
