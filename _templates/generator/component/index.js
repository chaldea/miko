module.exports = {
    params: ({ args }) => {
        const data = { ...args };
        data.context = JSON.parse(Buffer.from(args.json, 'base64').toString());
        delete data.json;
        return data;
    }
}
