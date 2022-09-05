const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');
const components = require('./components.json');
const slotted1 = /(?: )::slotted\(([\s\S]*?)\)(?!\))/gm;    // match ::slotted
const slotted2 = /(?:\):):slotted\(([\s\S]*?)\)(?!\))/gm;   // match ::slotted
const slotted3 = /::slotted\(([\s\S]*?)\)(?!\))/gm;         // match ::slotted
const host1 = /:host-context\(([\s\S]*?)\)(?!\))/gm;        // match :host-context
const host2 = /:host(?!-)(?:\(([\s\S]*?)\))*(\s|,|::)/gm;   // match :host

function transform(name, content) {
    return content
        .replace(slotted1, ` slot $1`)
        .replace(slotted2, `) slot $1`)
        .replace(slotted3, `${name} slot $1`)
        .replace(host1, `$1 ${name}`)
        .replace(host2, `${name}$1$2`)
}

function generateCss() {
    const srcPath = '../../ionic/ionic-framework/core';
    for (const component of components) {
        const componentPath = path.resolve(component.root ? component.root : srcPath, component.srcDir);
        const fileNames = fs.readdirSync(componentPath);
        for (let fileName of fileNames) {
            // 跳过无需处理的文件类型
            if (path.extname(fileName) !== component.include) {
                continue;
            }
            const specific = component.specifics.find(x => x.name === fileName);
            const srcFile = path.resolve(componentPath, fileName);
            let distFile = path.resolve(component.srcDir, fileName);
            if (specific) {
                if (specific.extname) {
                    fileName = fileName.replace(path.extname(fileName), specific.extname);
                    distFile = distFile.replace(path.extname(distFile), specific.extname);
                }

                if (specific.transform) {
                    const name = specific.suffix ? `${component.tag}${specific.suffix}` : component.tag;
                    const file = fs.readFileSync(srcFile, 'utf8');
                    const content = transform(name, file);
                    console.log(distFile);
                    fs.writeFileSync(distFile, content);
                } else {
                    fs.copyFileSync(srcFile, distFile);
                }
            } else {
                fs.copyFileSync(srcFile, distFile);
            }
        }
    }

    const context = {
        path: 'src/css/components.scss',
    }
    execSync(`"./node_modules/.bin/hygen" generator style style --json=${Buffer.from(JSON.stringify(context)).toString('base64')}`);
}

function generateIcons() {
    const context = {
        path: 'src/components/icon/IconResource.cs',
        svgRoot: '../../ionic/ionicons/src/svg'
    }
    execSync(`"./node_modules/.bin/hygen" generator icon icon --json=${Buffer.from(JSON.stringify(context)).toString('base64')}`);
}

function generate() {
    generateCss();
    generateIcons();
}

generate();