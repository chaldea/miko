const fs = require('fs');
const path = require('path');

module.exports = {
    params: ({ args }) => {
        const data = { ...args };
        data.context = JSON.parse(Buffer.from(args.json, 'base64').toString());
        const json = fs.readFileSync('./components.json', 'utf-8');
        const components = JSON.parse(json);
        const imports = [];
        for (const component of components) {
            for (const s of component.specifics) {
                if (s.import) {
                    let name = s.name;
                    if (s.extname) {
                        name = name.replace(path.extname(name), s.extname);
                    }
                    imports.push(`${component.srcDir.replace('src', '..')}/${name}`);
                }
            }
        }
        data.context.imports = imports;
        delete data.json;
        return data;
    }
}
